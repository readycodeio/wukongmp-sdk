using System.Collections;
using System.Collections.Generic;
using Friflo.Engine.ECS;
using WukongMp.Sdk.Api.Implementation;

namespace WukongMp.Sdk;

public readonly struct EntityList<T> : IEnumerable<T>
    where T : struct, IReadyEntity<T>
{
    public struct Enumerator : IEnumerator<T>
    {
        private readonly WukongSynchronizationApi _api;
        private readonly EntityList _entityList;

        private int _index;
        private T _current;

        internal Enumerator(WukongSynchronizationApi api, EntityList entityList)
        {
            _api = api;
            _entityList = entityList;
            _index = -1;
            _current = default!;
        }

        public readonly T Current => _current;

        readonly object IEnumerator.Current => Current;

        public bool MoveNext()
        {
            if (++_index >= _entityList.Count)
                return false;

            var entity = _entityList[_index];
            _current = default(T).Construct(_api, entity);
            return true;
        }

        public void Reset()
        {
            _index = -1;
            _current = default!;
        }

        public readonly void Dispose() { }
    }

    private readonly WukongSynchronizationApi _api;
    private readonly EntityList _entityList;

    internal EntityList(WukongSynchronizationApi api, EntityList entityList)
    {
        _api = api;
        _entityList = entityList;
    }

    public Enumerator GetEnumerator()
        => new(_api, _entityList);

    IEnumerator<T> IEnumerable<T>.GetEnumerator()
        => new Enumerator(_api, _entityList);

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();
}