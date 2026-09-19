using FluentResults;
using FluentValidation;
using My.XXX.Persistence.Interfaces;
using My.XXX.Service.DTOs;
using My.XXX.Service.Interfaces;
using My.XXX.Service.Mapping;
using My.XXX.Shared;
using System.Linq;

namespace My.XXX.Service
{
    public class DemoService : IDemoService, IScopeDependency
    {
        private readonly IValidator<DemoModel> _validator;
        private readonly IDemoRepository _demoRepository;
        private readonly ApplicationMapper _mapper;

        public DemoService(
            IValidator<DemoModel> validator,
            IDemoRepository demoRepository,
            ApplicationMapper mapper)
        {
            _demoRepository = demoRepository;
            _validator = validator;
            _mapper = mapper;
        }

        public Result Save(DemoModel model)
        {
            var val = _validator.Validate(model);
            if (!val.IsValid)
            {
                return Result.Fail(val.Errors.First().ErrorMessage);
            }

            var demo = _mapper.ToDemo(model);
            var details = _mapper.ToDemoDetails(model.Details);

            var result = _demoRepository.Add(demo, details);
            return result ? Result.Ok() : Result.Fail("Save failed.");
        }

        public Result Update(DemoModel model)
        {
            var demo = _mapper.ToDemo(model);
            var value = _demoRepository.Upate(demo);
            return value > 0 ? Result.Ok() : Result.Fail("Save failed.");
        }

        public void ExecProc()
        {
            _demoRepository.QueryProcMultiple();
        }
    }
}