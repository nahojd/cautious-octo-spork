using System;
using Elsa.Activities.Http.Activities;
using Elsa.Expressions;
using Elsa.Services;
using Elsa.Services.Models;

namespace ElsaWorkflow
{
	public class TestWorkflow : IWorkflow
	{
		public void Build(IWorkflowBuilder builder)
		{
			builder
				.StartWith<ReceiveHttpRequest>(x => {
					x.Method = "POST";
					x.Path = new Uri("/register", UriKind.Relative);
					x.ReadContent = true;
					x.Name = "Registrera";
				})
				.Then<WriteHttpResponse>(x => {
					x.Content = new LiteralExpression("Ditt ärende är mottaget, var god lägg på luren.");
					x.ContentType = "text/plain";
					x.StatusCode = System.Net.HttpStatusCode.OK;
				});	
		}
	}
}