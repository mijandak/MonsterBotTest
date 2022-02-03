// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
//
// Generated with Bot Builder V4 SDK Template for Visual Studio CoreBot v4.15.0

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using Microsoft.Recognizers.Text.DataTypes.TimexExpression;

namespace CoreBot1.Dialogs
{
    public class MainDialog : ComponentDialog
    {
        private readonly FlightBookingRecognizer _luisRecognizer;
        protected readonly ILogger Logger;
        private JobDetails _jobDetails;

        // Dependency injection uses this constructor to instantiate MainDialog
        public MainDialog(FlightBookingRecognizer luisRecognizer, ILogger<MainDialog> logger)
            : base(nameof(MainDialog))
        {
            _luisRecognizer = luisRecognizer;
            Logger = logger;

            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new GetLocationDialog("GetMissingInfo", _luisRecognizer));
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                IntroStepAsync,
                ActStepAsync,
                FinalStepAsync,
            }));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> IntroStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if (!_luisRecognizer.IsConfigured)
            {
                await stepContext.Context.SendActivityAsync(
                    MessageFactory.Text("NOTE: LUIS is not configured. To enable all capabilities, add 'LuisAppId', 'LuisAPIKey' and 'LuisAPIHostName' to the appsettings.json file.", inputHint: InputHints.IgnoringInput), cancellationToken);

                return await stepContext.NextAsync(null, cancellationToken);
            }

            // Use the text provided in FinalStepAsync or the default if it is the first time.
            var messageText = stepContext.Options?.ToString() ?? "Welcome to Monster Job Search. What job are you looking for?";
            var promptMessage = MessageFactory.Text(messageText, InputHints.ExpectingInput);
            return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
        }

        private async Task<DialogTurnResult> ActStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if (!_luisRecognizer.IsConfigured)
            {
                //some stuff when it LUIS is not configured
            }

            // Call LUIS. (Note the TurnContext has the response to the prompt.)
            var luisResult = await _luisRecognizer.RecognizeAsync<MonsterJobSearchTest>(stepContext.Context, cancellationToken);
            switch (luisResult.TopIntent().intent)
            {
                case MonsterJobSearchTest.Intent.findAJob:
                    if (luisResult.Entities.Profession == null)
                    {
                        if (luisResult.Entities.ProfessionAny != null)
                        {
                            return await stepContext.ReplaceDialogAsync(InitialDialogId, "Sorry, I don't know that profession. Please specify the job again.", cancellationToken);
                        }
                        return await stepContext.ReplaceDialogAsync(InitialDialogId, "Sorry, I don't undestand this. Please specify the job again.", cancellationToken);
                    }

                    string profession = luisResult.Entities.Profession[0][0];

                    _jobDetails = new JobDetails() { Job = profession };

                    if (luisResult.Entities.LocationAny == null)
                    {
                        return await stepContext.BeginDialogAsync("GetMissingInfo", _jobDetails, cancellationToken);
                    }
                    if (luisResult.Entities.geographyV2 == null)
                    {
                        return await stepContext.ReplaceDialogAsync(InitialDialogId, "Sorry, I don't know that location. Please specify the job again.", cancellationToken);
                    }

                    _jobDetails.Location = luisResult.Entities.geographyV2[0].Location;

                    break;
                case MonsterJobSearchTest.Intent.EndDialogML:
                    return await stepContext.EndDialogAsync();
                default:
                    return await stepContext.ReplaceDialogAsync(InitialDialogId, "Sorry, I didn't get that. Please try asking in a different way.", cancellationToken);

            }

            return await stepContext.NextAsync(_jobDetails, cancellationToken);
        }

        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if (stepContext.Result is JobDetails jobDetails)
            {
                var msg = MessageFactory.Text($"Here is a list of available {jobDetails.Job} positions in {jobDetails.Location} ...", InputHints.IgnoringInput);
                await stepContext.Context.SendActivityAsync(msg, cancellationToken);
                return await stepContext.ReplaceDialogAsync(InitialDialogId, "What other job are you looking for?", cancellationToken);
            }

            // Restart the main dialog with a different message the second time around
            var promptMessage = "Sorry, I didn't get that. Please try asking in a different way.";
            return await stepContext.ReplaceDialogAsync(InitialDialogId, promptMessage, cancellationToken);
        }
    }

    public class JobDetails
    {
        public string Job { get; set; }
        public string Location { get; set; }
    }
}
