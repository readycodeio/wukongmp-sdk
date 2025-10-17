using System.Diagnostics;
using System.Reflection;
using CSharpModBase;
using Microsoft.Extensions.Logging;
using WukongMp.Api;
using WukongMp.Api.Shim;

namespace WukongMp.Testing
{
    // ReSharper disable once UnusedType.Global
    public class Mod : ICSharpModExV2
    {
        public string Name => "WukongMp Tests";
        public string Version => "1.0.0";

        private ILogger _logger = null!;

        public bool IsDebug
#if DEBUG
            => true;
#else
            => false;
#endif

        public void SetLoggerFactory(ILoggerFactory loggerFactory)
        {
            DI.Instance.InitLogging(loggerFactory);
            _logger = DI.Instance.Logger;
        }

        public void Init()
        {
            if (!LaunchParameters.Instance.ValidForCoOp)
            {
                _logger.LogError("Multiplayer is disabled. Launch the game through the ReadyM Launcher to play WukongMP.");
                return;
            }

            DI.Instance.Init();

            if (LaunchParameters.Instance.PlayShimOnStart)
                ShimUtils.InitRelayPlayShim(
                    DI.Instance,
                    LaunchParameters.Instance.PlayShimFile!
                );
            else if (LaunchParameters.Instance.RecordShimOnStart)
                ShimUtils.InitRelayRecordShim(
                    DI.Instance,
                    LaunchParameters.Instance.ServerIp!,
                    LaunchParameters.Instance.ServerPort!.Value,
                    LaunchParameters.Instance.UserGuid,
#if NO_DISCONNECT
                    true,
#else
                    false,
#endif
                    LaunchParameters.Instance.RecordShimFile!
                );
            else
                ShimUtils.InitRelay(
                    DI.Instance,
                    "127.0.0.1",
                    9050,
                    LaunchParameters.Instance.UserGuid,
#if NO_DISCONNECT
                    true
#else
                    false
#endif
                );

            if (!DI.Instance.Patcher.IsPatched)
            {
                DI.Instance.Patcher.Patch();
            }
        }

        public void LateInit()
        {
            if (!LaunchParameters.Instance.ValidForCoOp)
            {
                _logger.LogError("Multiplayer is disabled. Launch the game through the ReadyM Launcher to play WukongMP.");
                return;
            }

            _logger.LogInformation("Init WukongMP mod");

            // InformationalVersion from assembly def
            var trueModVersion = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

            _logger.LogInformation("Mod version: {Version}", trueModVersion);
            _logger.LogInformation("Process name: {ProcessName}", Process.GetCurrentProcess().ProcessName);

            // NOTE: EcsLoop requires initialization from the same thread that will execute Tick()
            Utils.TryRunOnGameThread(() =>
            {
                Debug.Assert(DI.Instance.Patcher.IsPatched);

                if (!DI.Instance.Connection.IsRunning)
                {
                    DI.Instance.EcsLoop.Start();
                    DI.Instance.Connection.Start();
                }
                else
                {
                    _logger.LogError("WukongMP is already initialized");
                    return;
                }

                if (!DI.Instance.Connection.RequestedConnect)
                {
                    DI.Instance.Connection.Connect();
                }

                if (!DI.Instance.TestsRunner.IsRunning)
                {
                    DI.Instance.TestsRunner.Init(new Api.Tests.TestActionSequences.ReconnectTestsSequence(DI.Instance.Logger)); 
                    DI.Instance.TestsRunner.Start();
                }
            });
        }

        public void DeInit()
        {
            _logger.LogInformation("DeInit");

            if (!LaunchParameters.Instance.ValidForCoOp)
            {
                return;
            }

            Utils.TryRunOnGameThread(() =>
            {
                if (DI.Instance.TestsRunner.IsRunning)
                {
                    DI.Instance.TestsRunner.Stop();
                    DI.Instance.TestsRunner.Clear();
                }

                if (DI.Instance.Connection.RequestedConnect)
                {
                    DI.Instance.Connection.Disconnect();
                }

                if (DI.Instance.Connection.IsRunning)
                {
                    DI.Instance.Connection.Stop();
                    DI.Instance.EcsLoop.Stop();
                }

                if (DI.Instance.Patcher.IsPatched)
                {
                    DI.Instance.Patcher.Unpatch();
                }
            });
        }

        public object GetReloadContext()
        {
            _logger.LogInformation("GetReloadContext");
            return (bool?)DI.Instance.AreaState.InRoom;
        }

        public void Reload(object? context)
        {
            _logger.LogInformation("Reload");

            var connectedAndInRoom = context as bool?;
            if (connectedAndInRoom == true)
            {
                _logger.LogInformation("Reconnecting after a reload");
                DI.Instance.Connection.Reconnect();
            }
        }
    }
}