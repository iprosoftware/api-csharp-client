using System;
using System.Collections.Generic;

namespace iPro.SDK.Client.BatchJsons
{
    public class BatchJsonStore<TState>
        where TState : class
    {
        private readonly TState _state;
        private readonly List<Action<TState>> _subscribers;

        public BatchJsonStore(TState state)
        {
            _state = state;
            _subscribers = new List<Action<TState>>();
        }

        public BatchJsonStore<TState> Dispatch(Action<TState> setter)
        {
            setter.Invoke(_state);

            _subscribers.ForEach((subscriber) =>
            {
                subscriber(_state);
            });

            return this;
        }

        public BatchJsonStore<TState> Subscribe(Action<TState> subscriber)
        {
            _subscribers.Add(subscriber);
            return this;
        }
    }
}
