namespace OrderingShipping.SharedKernel;

public interface ICommandHandler<in TCommand, out TResult>
    where TCommand : ICommand<TResult>
{
    TResult Handle(TCommand command);
}
