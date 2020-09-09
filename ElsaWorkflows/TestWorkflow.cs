using System;
using System.Dynamic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Elsa;
using Elsa.Activities;
using Elsa.Activities.Console.Activities;
using Elsa.Activities.ControlFlow.Activities;
using Elsa.Activities.Email.Activities;
using Elsa.Activities.Http.Activities;
using Elsa.Activities.Workflows.Activities;
using Elsa.Attributes;
using Elsa.Expressions;
using Elsa.Results;
using Elsa.Scripting.JavaScript;
using Elsa.Scripting.Liquid;
using Elsa.Services;
using Elsa.Services.Models;
using Newtonsoft.Json;

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
				.Then<WriteLine>(x => {
					x.TextExpression = new LiteralExpression("Nu har vi kommit ända hit!");
				})
				.Then<SetVariable>(x => {
					x.VariableName = "Input";
					x.ValueExpression = new JavaScriptExpression<ExpandoObject>("lastResult().Body");
				})
				.Then<IfElse>(x =>
					x.ConditionExpression = new JavaScriptExpression<bool>("Input.number > 0"),
					ifElse => {
						ifElse.When(OutcomeNames.False)
							.Then<WriteHttpResponse>(x => {
								x.Content = new LiteralExpression("Ogiltig indata");
								x.ContentType = "text/plain";
								x.StatusCode = System.Net.HttpStatusCode.BadRequest;
							})
							.Then<Finish>(x => {})
							;

						ifElse.When(OutcomeNames.True)
							.Then<WriteHttpResponse>(x => {
								x.Content = new LiteralExpression("Ditt ärende är mottaget, var god lägg på luren.");
								x.ContentType = "text/plain";
								x.StatusCode = System.Net.HttpStatusCode.OK;
							})
							.Then<WriteLine>(x => {
								x.TextExpression = new LiteralExpression("Nu har vi kommit förbi ifelse!");
							})
							.Then<SetVariable>(x => {
								x.VariableName = "Id";
								x.ValueExpression = new JavaScriptExpression<int>("Math.floor(Math.random()*10000000)");
							})
							.Then<CalculateSquare>(x => {
								x.Number = new JavaScriptExpression<int>("Input.number");
								x.FunctionUrl = "https://func-apprch-mocks.azurewebsites.net/api/Square?code=DaDC6Ng8mNjrBwbaaC/FhMESiv/ItNIPQXYPzTYeXIFaXhoO2EAkDA==";
							})
							.Then<SetVariable>(x => {
								x.VariableName = "SquareResult";
								x.ValueExpression = new JavaScriptExpression<int>("lastResult()");
							})
							.Then<WriteLine>(x => {
								x.TextExpression = new LiquidExpression<string>("Resultat av square: {{ Variables.SquareResult }}");
							})
							.Then<SendEmail>(x => {
								x.From = new LiteralExpression<string>("test@example.com");
								x.To = new LiteralExpression<string>("oops@approach.se");
								x.Subject = new LiquidExpression<string>("Ny registrering från {{ Variables.Input.name }}");
								x.Body = new JavaScriptExpression<string>(
									"`Document from ${Input.name} received for review.\r\n" +
									"Number: ${Input.number}. Square: ${SquareResult}\r\n" +
									"<a href=\"${signalUrl('Approve')}\">Approve</a> or <a href=\"${signalUrl('Reject')}\">Reject</a>`"
								);
							})
							.Then<SetVariable>(x => {
								x.VariableName = "ApprovalStatus";
								x.ValueExpression = new LiteralExpression("pending");
							})
							.Then<Fork>(
								x => { x.Branches = new[] { "Approve", "Reject", "Remind" }; },
								fork =>
								{
									fork
										.When("Approve")
										.Then<Signaled>(x => x.Signal = new LiteralExpression("Approve"))
										.Then("Join");

									fork
										.When("Reject")
										.Then<Signaled>(x => x.Signal = new LiteralExpression("Reject"))
										.Then("Join");
								}
							)
							.Then<Join>(x => x.Mode = Join.JoinMode.WaitAny, name: "Join")
							.Then<SetVariable>(x => {
								x.VariableName = "ApprovalStatus";
								x.ValueExpression =  new JavaScriptExpression<object>("input('Signal') === 'Approve' ? 'approved' : 'rejected'");
							})
							.Then<WriteLine>(x => { x.TextExpression = new LiquidExpression<string>("Svar för {{ Variables.Input.name}}: {{ Variables.ApprovalStatus }}!"); })
							;
					});
				// .Then<WriteHttpResponse>(x => {
				// 	x.Content = new LiteralExpression("Ditt ärende är mottaget, var god lägg på luren.");
				// 	x.ContentType = "text/plain";
				// 	x.StatusCode = System.Net.HttpStatusCode.OK;
				// }); 
		}
	}

	[ActivityDefinition(
		Category = "Custom",
		DisplayName = "Calculate Square",
		Icon = "fas fa-calculator"
	)]
	public class CalculateSquare : Activity
	{
		private readonly IWorkflowExpressionEvaluator expressionEvaluator;

		public CalculateSquare(IWorkflowExpressionEvaluator expressionEvaluator)
		{
			this.expressionEvaluator = expressionEvaluator;
		}

		[ActivityProperty(Hint = "The number to square")]
		public WorkflowExpression<int> Number {
			get => GetState<WorkflowExpression<int>>();
			set => SetState(value);
		}

		[ActivityProperty(Hint = "Url till Azure function")]
		public string FunctionUrl {
			get => GetState<string>();
			set => SetState(value);
		}

		protected override async Task<ActivityExecutionResult> OnExecuteAsync(WorkflowExecutionContext context, CancellationToken cancellationToken)
		{
			var result = new SquareResult();

			var number = await expressionEvaluator.EvaluateAsync(Number, typeof(int), context, cancellationToken);

			using (var client = new HttpClient()) {
				var request = new HttpRequestMessage(HttpMethod.Post, FunctionUrl);
				var data = new { number };
				var json = JsonConvert.SerializeObject(data);
				request.Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

				var response = await client.SendAsync(request);
				var resultJson = await response.Content.ReadAsStringAsync();
				result = JsonConvert.DeserializeObject<SquareResult>(resultJson);
			}

			context.SetLastResult(Output.SetVariable("Square", result.Result));
			return Done();
		}

		public class SquareResult {
			public int Result { get; set; }
		}
	}
}