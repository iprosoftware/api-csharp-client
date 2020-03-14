using System;
using System.Collections.Generic;

namespace iPro.SDK.Client.BatchJsons
{
    public class BatchJsonStore<TState>
        where TState : new()
    {
        private readonly List<Action<TState>> _subscribers;

        public BatchJsonStore()
        {
            _subscribers = new List<Action<TState>>();
        }

        public TState State { get; private set; } = new TState();

        public BatchJsonStore<TState> Dispatch(Action<TState> setter)
        {
            setter.Invoke(State);

            _subscribers.ForEach((subscriber) =>
            {
                subscriber(State);
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
