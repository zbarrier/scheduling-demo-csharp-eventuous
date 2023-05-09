using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xunit;

namespace Eventuous.TestHelpers;
public abstract class AggregateTest<TAggregate, TState, TId>
    where TAggregate : Aggregate<TState>, new()
    where TState : State<TState>, new()
    where TId : AggregateId
{
    readonly Aggregate<TState> _aggregate;
    readonly IAggregateStore _aggregateStore;
    readonly CommandService<TAggregate, TState, TId> _commandService;

    Result<TState>? _result;
    Exception? _exception;

    protected AggregateTest(Func<IAggregateStore, CommandService<TAggregate, TState, TId>> commandService)
    {
        _aggregate = (Aggregate<TState>)Activator.CreateInstance(typeof(TAggregate))!;
        _aggregateStore = new MockAggregateStore(_aggregate);
        _commandService = commandService(_aggregateStore);

        _result = null;
        _exception = null;
    }

    protected void Given(params object[] events)
        => _aggregate.Load(events);

    protected async Task When<TCommand>(TCommand command) where TCommand : class
    {
        try
        {
            _aggregate.ClearChanges();
            _result = await _commandService.Handle(command, default);
        }
        catch (Exception exc)
        {
            _exception = exc;
        }
    }

    protected void Then(Action<List<Change>> changes)
    {
        if (_exception is not null)
            throw _exception;

        if (_result is null)
            throw new Exception("Unknown error occurred, both exception and result are null.");

        if (!_result.Success)
        {
            if (_result is ErrorResult errorResult)
            {
                throw new Exception(errorResult.ErrorMessage);
            }

            throw new Exception("Unknown error occurred.");
        }

        changes(_result.Changes?.ToList() ?? new List<Change>());
    }

    protected void Then(string expectedErrorMessage)
    {
        if (_exception is not null)
            throw _exception;

        if (_result is not null)
        {
            if (_result.Success)
                throw new Exception("Test was successful but an error was expected.");

            if (_result is ErrorResult<TState> errorResultWithState)
            {
                Assert.Equal(expectedErrorMessage, errorResultWithState.ErrorMessage);
                return;
            }
            else if (_result is ErrorResult errorResult)
            {
                Assert.Equal(expectedErrorMessage, errorResult.ErrorMessage);
                return;
            }

            throw new Exception("Test was not successful, but result is not an ErrorResult.");
        }

        throw new Exception("Unknown error occurred, both exception and result are null.");
    }

    protected void Then<TException>() where TException : Exception
    {
        if (_exception is not null)
        {
            Assert.Equal(typeof(TException), _exception.GetType());
            return;
        }

        if (_result is not null)
        {
            if (_result.Success)
                throw new Exception("Test was successful but an error was expected.");

            if (_result is ErrorResult<TState> errorResultWithState)
            {
                if (errorResultWithState.Exception is null)
                    throw new Exception("Test failed but an exception was expected.");

                Assert.Equal(typeof(TException), errorResultWithState.Exception.GetType());
                return;
            }
            else if (_result is ErrorResult errorResult)
            {
                if (errorResult.Exception is null)
                    throw new Exception("Test failed but an exception was expected.");

                Assert.Equal(typeof(TException), errorResult.Exception.GetType());
                return;
            }

            throw new Exception("Test was not successful, but result is not an ErrorResult.");
        }

        throw new Exception("Unknown error occurred, both exception and result are null.");
    }

    protected void Then<TException>(string expectedErrorMessage) where TException : Exception
    {
        if (_exception is not null)
        {
            Assert.Equal(typeof(TException), _exception.GetType());
            Assert.Equal(expectedErrorMessage, _exception.Message);
            return;
        }

        if (_result is not null)
        {
            if (_result.Success)
                throw new Exception("Test was successful but an error was expected.");

            if (_result is ErrorResult<TState> errorResultWithState)
            {
                if (errorResultWithState.Exception is null)
                    throw new Exception("Test failed but an exception was expected.");

                Assert.Equal(typeof(TException), errorResultWithState.Exception.GetType());
                Assert.Equal(expectedErrorMessage, errorResultWithState.Exception.Message);
                return;
            }
            else if (_result is ErrorResult errorResult)
            {
                if (errorResult.Exception is null)
                    throw new Exception("Test failed but an exception was expected.");

                Assert.Equal(typeof(TException), errorResult.Exception.GetType());
                Assert.Equal(expectedErrorMessage, errorResult.Exception.Message);
                return;
            }

            throw new Exception("Test was not successful, but result is not an ErrorResult.");
        }

        throw new Exception("Unknown error occurred, both exception and result are null.");
    }

    protected void Then<TException>(TException expectedException) where TException : Exception
    {
        if (_exception is not null)
        {
            Assert.Equal(typeof(TException), _exception.GetType());
            Assert.Equal(expectedException.Message, _exception.Message);
            return;
        }

        if (_result is not null)
        {
            if (_result.Success)
                throw new Exception("Test was successful but an error was expected.");

            if (_result is ErrorResult<TState> errorResultWithState)
            {
                if (errorResultWithState.Exception is null)
                    throw new Exception("Test failed but an exception was expected.");

                Assert.Equal(typeof(TException), errorResultWithState.Exception.GetType());
                Assert.Equal(expectedException.Message, errorResultWithState.Exception.Message);
                return;
            }
            else if (_result is ErrorResult errorResult)
            {
                if (errorResult.Exception is null)
                    throw new Exception("Test failed but an exception was expected.");

                Assert.Equal(typeof(TException), errorResult.Exception.GetType());
                Assert.Equal(expectedException.Message, errorResult.Exception.Message);
                return;
            }

            throw new Exception("Test was not successful, but result is not an ErrorResult.");
        }

        throw new Exception("Unknown error occurred, both exception and result are null.");
    }
}
