using DevStore.Core.Communication;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Collections.Generic;
using System.Linq;

namespace DevStore.WebAPI.Core.Controllers
{
    [ApiController]
    public abstract class MainController : Controller
    {
        protected ICollection<string> Errors = new List<string>();

        protected ActionResult CustomResponse(object result = null)
        {
            if (ValidOperation())
            {
                return Ok(result);
            }

            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                { "Messages", Errors.ToArray() }
            }));
        }

        protected ActionResult CustomResponse(ModelStateDictionary modelState)
        {
            var erros = modelState.Values.SelectMany(e => e.Errors);
            foreach (var erro in erros)
            {
                AddErrorToStack(erro.ErrorMessage);
            }

            return CustomResponse();
        }

        protected ActionResult CustomResponse(ValidationResult validationResult)
        {
            foreach (var erro in validationResult.Errors)
            {
                AddErrorToStack(erro.ErrorMessage);
            }

            return CustomResponse();
        }

        protected ActionResult CustomResponse(ResponseResult resposta)
        {
            ResponsePossuiErros(resposta);

            return CustomResponse();
        }

        protected bool ResponsePossuiErros(ResponseResult resposta)
        {
            if (resposta == null || !resposta.Errors.Messages.Any()) return false;

            foreach (var mensagem in resposta.Errors.Messages)
            {
                AddErrorToStack(mensagem);
            }

            return true;
        }

        protected bool ValidOperation()
        {
            return !Errors.Any();
        }

        protected void AddErrorToStack(string erro)
        {
            Errors.Add(erro);
        }

        protected void CleanErrors()
        {
            Errors.Clear();
        }
    }
}