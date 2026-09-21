using My.XXX.Contracts.DTOs;
namespace My.XXX.Services.Ports;

public interface IDemoRepository
{
    bool Add(DemoModel model);
    int Update(DemoModel model);
    void QueryProcMultiple();
}
