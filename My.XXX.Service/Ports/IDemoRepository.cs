using My.XXX.Service.DTOs;
namespace My.XXX.Service.Ports;

public interface IDemoRepository
{
    bool Add(DemoModel model);
    int Update(DemoModel model);
    void QueryProcMultiple();
}
