using My.XXX.Infra;
using My.XXX.Service.DTOs;

namespace My.XXX.Service.Interfaces
{
    public interface IDemoService
    {
        PwCResult Save(DemoModel model);

        PwCResult Update(DemoModel model);

        public void ExecProc();
    }
}