using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;

namespace CoreBot1.Dialogs
{
	public class GetLocationDialog : ComponentDialog
	{
		FlightBookingRecognizer _luisRecognizer;
        private JobDetails jobDetails;

        public GetLocationDialog(string dialogId, FlightBookingRecognizer luisRecognizer) : base(dialogId)
		{
			_luisRecognizer = luisRecognizer;

			AddDialog(new TextPrompt(nameof(TextPrompt)));
			AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
			{
				step1,
				step2
			}));

			InitialDialogId = nameof(WaterfallDialog);
		}

		private async Task<DialogTurnResult> step2(WaterfallStepContext stepContext, CancellationToken cancellationToken)
		{
			var luisResult = await _luisRecognizer.RecognizeAsync<MonsterJobSearchTest>(stepContext.Context, cancellationToken);
			if (luisResult.Entities.geographyV2 == null)
			{
				return await stepContext.EndDialogAsync(null, cancellationToken);
			}
            else
            {
				jobDetails.Location = luisResult.Entities.geographyV2[0].Location;
				return await stepContext.EndDialogAsync(jobDetails, cancellationToken);
			}
		}

		private async Task<DialogTurnResult> step1(WaterfallStepContext stepContext, CancellationToken cancellationToken)
		{
			jobDetails = (JobDetails)stepContext.Options;
			var promptMessage = MessageFactory.Text($"Where are you looking for {jobDetails.Job} jobs?", InputHints.ExpectingInput);
			return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
		}
	}
}
