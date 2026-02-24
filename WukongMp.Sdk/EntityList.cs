using System.Collections;
using System.Collections.Generic;
using Friflo.Engine.ECS;
using WukongMp.Sdk.Api;

namespace WukongMp.Sdk;

public readonly struct EntityList<T> : IEnumerable<T>
    where T : struct, IReadyEntity<T>
{
    public struct Enumerator : IEnumerator<T>
    {
        private readonly WukongClientApi _api;
        private readonly EntityList _entityList;
        
        private int _index;
        private T _current; 

        internal Enumerator(WukongClientApi api, EntityList entityList)
        {
            _api = api;
            _entityList = entityList;
            _index = -1;
            _current = default!;
        }

        public T Current => _current;

        object? IEnumerator.Current => Current;

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

        public void Dispose()
        {
        }
    }
    
    private readonly WukongClientApi _api;
    private readonly EntityList _entityList;
    
    internal EntityList(WukongClientApi api, EntityList entityList)
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