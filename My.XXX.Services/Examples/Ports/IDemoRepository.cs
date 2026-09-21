using My.XXX.Contracts.DTOs;
namespace My.XXX.Services.Examples.Ports;

public interface IDemoRepository
{
    bool Add(DemoModel model);
    int Update(DemoModel model);
    void QueryProcMultiple();
}
