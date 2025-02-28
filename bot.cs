using System;
using System.Globalization;
using System.Collections.Generic;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace SimpleCalendarBot
{
    class Program
    {
        // The bot client
        static ITelegramBotClient bot;

        // The Main method starts the bot
        static async Task Main(string[] args)
        {
            // Get the token from the environment variable
            string token = Environment.GetEnvironmentVariable("TELEGRAM_TOKEN");
            if (string.IsNullOrEmpty(token))
            {
                Console.WriteLine("Please set the TELEGRAM_TOKEN environment variable.");
                return;
            }

            bot = new TelegramBotClient(token);
            var me = await bot.GetMeAsync();
            Console.WriteLine("Bot is running as " + me.Username);

            // Subscribe to message and callback events
            bot.OnMessage += Bot_OnMessage;
            bot.OnCallbackQuery += Bot_OnCallbackQuery;
            bot.StartReceiving();

            Console.WriteLine("Press any key to exit.");
            Console.ReadKey();
            bot.StopReceiving();
        }

        // This method handles text messages from users.
        private static async void Bot_OnMessage(object sender, MessageEventArgs e)
        {
            // Check if the message is text.
            if (e.Message.Type != MessageType.Text)
                return;

            // If the message is "/calendar"
            if (e.Message.Text.ToLower() == "/calendar")
            {
                DateTime now = DateTime.Now;
                // Build a calendar for the current month
                InlineKeyboardMarkup keyboard = BuildCalendar(now.Year, now.Month);
                await bot.SendTextMessageAsync(
                    e.Message.Chat.Id,
                    "Please choose a date:",
                    replyMarkup: keyboard
                );
            }
            else
            {
                // If the message is not /calendar
                await bot.SendTextMessageAsync(
                    e.Message.Chat.Id,
                    "Send /calendar to see the calendar."
                );
            }
        }

        // This method handles button clicks (callback queries).
        private static async void Bot_OnCallbackQuery(object sender, CallbackQueryEventArgs e)
        {
            string data = e.CallbackQuery.Data;
            
            // If a day is chosen. Format: DAY_year_month_day
            if (data.StartsWith("DAY_"))
            {
                string[] parts = data.Split('_');
                int year = int.Parse(parts[1]);
                int month = int.Parse(parts[2]);
                int day = int.Parse(parts[3]);
                DateTime date = new DateTime(year, month, day);
                await bot.AnswerCallbackQueryAsync(e.CallbackQuery.Id, "You chose: " + date.ToShortDateString());
            }
            // If navigation button is clicked. Format: NAV_year_month
            else if (data.StartsWith("NAV_"))
            {
                string[] parts = data.Split('_');
                int year = int.Parse(parts[1]);
                int month = int.Parse(parts[2]);
                InlineKeyboardMarkup keyboard = BuildCalendar(year, month);
                await bot.EditMessageReplyMarkupAsync(
                    e.CallbackQuery.Message.Chat.Id,
                    e.CallbackQuery.Message.MessageId,
                    keyboard
                );
                await bot.AnswerCallbackQueryAsync(e.CallbackQuery.Id);
            }
        }

        // This method builds the calendar keyboard.
        private static InlineKeyboardMarkup BuildCalendar(int year, int month)
        {
            // Get the first day of the month
            DateTime firstDay = new DateTime(year, month, 1);
            // Get the number of days in the month
            int daysInMonth = DateTime.DaysInMonth(year, month);
            // Get the day of the week of the first day (0 = Sunday)
            int startDay = (int)firstDay.DayOfWeek;

            var rows = new List<InlineKeyboardButton[]>();

            // Header row with navigation buttons
            DateTime prevMonth = firstDay.AddMonths(-1);
            DateTime nextMonth = firstDay.AddMonths(1);
            var headerRow = new InlineKeyboardButton[]
            {
                InlineKeyboardButton.WithCallbackData("<", $"NAV_{prevMonth.Year}_{prevMonth.Month}"),
                InlineKeyboardButton.WithCallbackData(firstDay.ToString("MMMM yyyy", CultureInfo.InvariantCulture), "IGNORE"),
                InlineKeyboardButton.WithCallbackData(">", $"NAV_{nextMonth.Year}_{nextMonth.Month}")
            };
            rows.Add(headerRow);

            // Row with day names (optional)
            string[] dayNames = { "Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat" };
            var dayNameRow = new InlineKeyboardButton[7];
            for (int i = 0; i < 7; i++)
            {
                dayNameRow[i] = InlineKeyboardButton.WithCallbackData(dayNames[i], "IGNORE");
            }
            rows.Add(dayNameRow);

            // Rows with day numbers
            int currentDay = 1;
            for (int week = 0; week < 6; week++)
            {
                var dayRow = new List<InlineKeyboardButton>();
                for (int d = 0; d < 7; d++)
                {
                    // Add empty buttons before the first day of the month
                    if (week == 0 && d < startDay)
                    {
                        dayRow.Add(InlineKeyboardButton.WithCallbackData(" ", "IGNORE"));
                    }
                    else if (currentDay > daysInMonth)
                    {
                        // Add empty buttons after the last day
                        dayRow.Add(InlineKeyboardButton.WithCallbackData(" ", "IGNORE"));
                    }
                    else
                    {
                        // Add a button with the day number.
                        dayRow.Add(InlineKeyboardButton.WithCallbackData(
                            currentDay.ToString(),
                            $"DAY_{year}_{month}_{currentDay}"
                        ));
                        currentDay++;
                    }
                }
                rows.Add(dayRow.ToArray());
                if (currentDay > daysInMonth)
                    break;
            }

            return new InlineKeyboardMarkup(rows);
        }
    }
}
