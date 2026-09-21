using My.XXX.Service.DTOs;
namespace My.XXX.Services.Ports;

public interface IDemoRepository
{
    bool Add(DemoModel model);
    int Update(DemoModel model);
    void QueryProcMultiple();
}
