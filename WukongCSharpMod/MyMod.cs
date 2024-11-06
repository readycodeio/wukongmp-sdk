using System;
using System.Diagnostics;
using CSharpModBase;
using CSharpModBase.Input;

namespace WukongCSharpMod
{
    public class MyMod : ICSharpMod
    {
        public string Name => "ModExample";
        public string Version => "0.0.1";

        public void Init()
        {
            Console.WriteLine("Init");
        
            Utils.RegisterKeyBind(ModifierKeys.Alt, Key.X, () =>
            {
                Console.WriteLine("Alt + X");

                var playerCharacter = GameUtils.GetBGUPlayerCharacterCS();
                if (playerCharacter != null)
                {
                    var pawn = playerCharacter.GetController().GetControlledPawn();
                    var t = pawn.GetActorTransform();
                    var loc = t.GetLocation();
                    loc.Z += 1000f;
                    t.SetLocation(loc);
                    if (!pawn.SetActorTransform(t, true, out _, true))
                    {
                        Debug.WriteLine("Failed to teleport player.");
                    }
                }
            });
        }

        public void DeInit()
        {
            Console.WriteLine("DeInit");
        }
    }    
}
