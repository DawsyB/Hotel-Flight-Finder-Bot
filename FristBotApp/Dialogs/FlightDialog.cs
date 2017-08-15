using System;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using System.Collections.Generic;
using Microsoft.Bot.Builder.FormFlow;
using System.Web;
using System.Linq;

namespace HotelFlightFinder.Dialogs
{
    [Serializable]
    public class FlightDialog : IDialog<object>
    {
        public async Task StartAsync(IDialogContext context)
        {
            await context.PostAsync("Welcome to the Flight Finder!");
            var FlightFormsDialog = FormDialog.FromForm(this.BuildFlightForm, FormOptions.PromptInStart);
            context.Call(FlightFormsDialog, this.ResumeAfterFlightFormDialog);
        }

     

        private IForm<FlightsQuery> BuildFlightForm()
        {
            OnCompletionAsyncDelegate<FlightsQuery> processFlightSearch = async (context, state) =>
            {
                await context.PostAsync($"Ok. Searching for Flights in {state.Destination} from {state.FlyDate.ToString("MM/dd")} to {state.ReturnDate.ToString("MM/dd")}...");
            };

            return new FormBuilder<FlightsQuery>()
                .Field(nameof(FlightsQuery.Destination))
                .Field(nameof(FlightsQuery.FlyFrom))
                .Message("Looking for Flights travelling from {FlyFrom} to {Destination}...interesting!!!")
                .AddRemainingFields()
                .Confirm("Do you confirm the below booking details? \n Travelling from {FlyFrom} to {Destination} \n on {FlyDate} and return on {ReturnDate}")
                .Message("Thanks for confirming!!!")
                .OnCompletion(processFlightSearch)
                .Build();
                
        }

        private async Task ResumeAfterFlightFormDialog(IDialogContext context, IAwaitable<FlightsQuery> result)
        {
            var searchQuery = await result;
            var flights = await this.GetHotelsAsync(searchQuery);
            await context.PostAsync($"I found in total {flights.Count()} flights for your dates:");

            var resultMessage = context.MakeMessage();
            resultMessage.AttachmentLayout = AttachmentLayoutTypes.Carousel;
            resultMessage.Attachments = new List<Attachment>();

            foreach (var flight in flights)
            {
                HeroCard heroCard = new HeroCard()
                {
                    Title = $"{flight.Name} ({flight.Source}-{flight.Destination})",                    
                    Subtitle = $"{flight.Rating} stars. {flight.NumberOfReviews} reviews. From ${flight.PriceStarting}.",
                    Images = new List<CardImage>()
                        {
                            new CardImage() { Url = flight.Image }
                        },
                    Buttons = new List<CardAction>()
                        {
                            new CardAction()
                            {
                                Title = "More details",
                                Type = ActionTypes.OpenUrl,
                                Value = $"https://www.bing.com/search?q=flights+in+" + HttpUtility.UrlEncode(flight.Destination)
                            }
                        }
                };

                resultMessage.Attachments.Add(heroCard.ToAttachment());
            }

            await context.PostAsync(resultMessage);
            context.Done<object>(null);
        }

        private async Task<IEnumerable<Flight>> GetHotelsAsync(FlightsQuery searchQuery)
        {
            var flights = new List<Flight>();

            // Filling the flights results manually just for demo purposes
            for (int i = 1; i <= 5; i++)
            {
                var random = new Random(i);
                Flight flight = new Flight()
                {
                    Name = $"{searchQuery.Destination} Flight {i}",
                    Source = searchQuery.FlyFrom,
                    Destination = searchQuery.Destination,
                    Rating = random.Next(1, 5),
                    NumberOfReviews = random.Next(0, 5000),
                    PriceStarting = random.Next(80, 450),
                    Image = $"https://placeholdit.imgix.net/~text?txtsize=35&txt=Flights+{i}&w=500&h=260"
                };

                flights.Add(flight);
            }

            flights.Sort((h1, h2) => h1.PriceStarting.CompareTo(h2.PriceStarting));

            return flights;
        }
    }
}
