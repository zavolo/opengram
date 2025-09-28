namespace MyTelegram.Domain.Sagas;

public class SignInSaga :
    MyInMemoryAggregateSaga<SignInSaga, SignInSagaId, SignInSagaLocator>,
    ISagaIsStartedBy<AppCodeAggregate, AppCodeId, CheckSignInCodeCompletedEvent>,
    ISagaHandles<UserAggregate, UserId, CheckUserStatusCompletedEvent>,
    IApply<SignInSuccessSagaEvent>,
    IApply<SignUpRequiredSagaEvent>,
    IApply<SignInFailedSagaEvent>
{
    private readonly SignInSagaState _state = new();
    
    public SignInSaga(SignInSagaId id, IEventStore eventStore) : base(id, eventStore)
    {
        Register(_state);
    }

    public void Apply(SignInSuccessSagaEvent aggregateEvent)
    {
        CompleteAsync();
    }

    public void Apply(SignUpRequiredSagaEvent aggregateEvent)
    {
        CompleteAsync();
    }

    public void Apply(SignInFailedSagaEvent aggregateEvent)
    {
        CompleteAsync();
    }

    public Task HandleAsync(IDomainEvent<UserAggregate, UserId, CheckUserStatusCompletedEvent> domainEvent,
        ISagaContext sagaContext,
        CancellationToken cancellationToken)
    {
        if (domainEvent.AggregateEvent.IsUserLocked)
        {
            Emit(new SignInFailedSagaEvent(_state.RequestInfo, "USER_DEACTIVATED"));
            return Task.CompletedTask;
        }

        Emit(new SignInSuccessSagaEvent(_state.RequestInfo,
            _state.RequestInfo.AuthKeyId,
            _state.RequestInfo.PermAuthKeyId,
            domainEvent.AggregateEvent.UserId,
            domainEvent.AggregateEvent.AccessHash,
            domainEvent.AggregateEvent.UserId == 0,
            domainEvent.AggregateEvent.PhoneNumber,
            domainEvent.AggregateEvent.FirstName,
            domainEvent.AggregateEvent.LastName,
            domainEvent.AggregateEvent.HasPassword));
        
        return Task.CompletedTask;
    }

    public async Task HandleAsync(IDomainEvent<AppCodeAggregate, AppCodeId, CheckSignInCodeCompletedEvent> domainEvent,
        ISagaContext sagaContext,
        CancellationToken cancellationToken)
    {
        if (!domainEvent.AggregateEvent.IsCodeValid)
        {
            Emit(new SignInFailedSagaEvent(domainEvent.AggregateEvent.RequestInfo, "PHONE_CODE_INVALID"));
            return;
        }
        
        if (domainEvent.AggregateEvent.UserId == 0)
        {
            Emit(new SignUpRequiredSagaEvent(domainEvent.AggregateEvent.RequestInfo));
            return;
        }
        Emit(new SignInStartedSagaEvent(domainEvent.AggregateEvent.RequestInfo));
        var userId = UserId.Create(domainEvent.AggregateEvent.UserId);
        var checkUserStatusCommand = new CheckUserStatusCommand(userId, domainEvent.AggregateEvent.RequestInfo);
        Publish(checkUserStatusCommand);
    }
}

public class SignInSagaState : AggregateState<SignInSaga, SignInSagaId, SignInSagaState>,
    IApply<SignInStartedSagaEvent>
{
    public RequestInfo RequestInfo { get; private set; } = null!;

    public void Apply(SignInStartedSagaEvent aggregateEvent)
    {
        RequestInfo = aggregateEvent.RequestInfo;
    }
}

public class SignInFailedSagaEvent : RequestAggregateEvent2<SignInSaga, SignInSagaId>
{
    public string ErrorCode { get; }
   
    public SignInFailedSagaEvent(RequestInfo requestInfo, string errorCode)
        : base(requestInfo)
    {
        ErrorCode = errorCode;
    }
}