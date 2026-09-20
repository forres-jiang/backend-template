using My.XXX.Service.DTOs;
using My.XXX.Shared;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
namespace My.XXX.Service.Ports;
public interface IDemoRepository
{
    bool Add(DemoModel model);
    int Update(DemoModel model);
    void QueryProcMultiple();
}
