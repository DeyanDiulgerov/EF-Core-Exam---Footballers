namespace Footballers.DataProcessor
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Xml.Serialization;
    using Data;
    using Footballers.Data.Models;
    using Footballers.Data.Models.Enums;
    using Footballers.DataProcessor.ImportDto;
    using Newtonsoft.Json;

    public class Deserializer
    {
        private const string ErrorMessage = "Invalid data!";

        private const string SuccessfullyImportedCoach
            = "Successfully imported coach - {0} with {1} footballers.";

        private const string SuccessfullyImportedTeam
            = "Successfully imported team - {0} with {1} footballers.";

        public static string ImportCoaches(FootballersContext context, string xmlString)
        {
            var output = new StringBuilder();

            var xmlSerializer = new XmlSerializer(typeof(XmlCoachImportDto[]),
                                new XmlRootAttribute("Coaches"));

            var coaches = (XmlCoachImportDto[])xmlSerializer.Deserialize
                               (new StringReader(xmlString));

            var validCoaches = new List<Coach>();

            foreach (var xmlCoach in coaches)
            {
                if(!IsValid(xmlCoach))
                {
                    output.AppendLine(ErrorMessage);
                    continue;
                }


                var ourCoach = new Coach()
                {
                    Name = xmlCoach.Name,
                    Nationality = xmlCoach.Nationality
                };

                foreach (var xmlFootballer in xmlCoach.Footballers)
                {
                    if(!IsValid(xmlFootballer))
                    {
                        output.AppendLine(ErrorMessage);
                        continue;
                    }

                    DateTime startDate;
                    bool isStartDateValid = DateTime.TryParseExact
                        (xmlFootballer.ContractStartDate, "dd/MM/yyyy",
                        CultureInfo.InvariantCulture, DateTimeStyles.None, out startDate);

                    if (!isStartDateValid)
                    {
                        output.AppendLine(ErrorMessage);
                        continue;
                    }

                    DateTime endDate;
                    bool isEndDateValid = DateTime.TryParseExact
                        (xmlFootballer.ContractEndDate, "dd/MM/yyyy",
                        CultureInfo.InvariantCulture, DateTimeStyles.None, out endDate);

                    if (!isEndDateValid)
                    {
                        output.AppendLine(ErrorMessage);
                        continue;
                    }

                    if (startDate >= endDate)
                    {
                        output.AppendLine(ErrorMessage);
                        continue;
                    }

                    var ourFootballer = new Footballer()
                    {
                        Name = xmlFootballer.Name,
                        ContractStartDate = startDate,
                        ContractEndDate = endDate,
                        BestSkillType = (BestSkillType)xmlFootballer.BestSkillType,
                        PositionType = (PositionType)xmlFootballer.PositionType
                    };
                    ourCoach.Footballers.Add(ourFootballer);
                }
                validCoaches.Add(ourCoach);
                output.AppendLine($"Successfully imported coach - {ourCoach.Name}" +
                    $" with {ourCoach.Footballers.Count()} footballers.");

            }
            context.AddRange(validCoaches);
            context.SaveChanges();

            return output.ToString().TrimEnd();
        }
        public static string ImportTeams(FootballersContext context, string jsonString)
        {
            var output = new StringBuilder();

            var teams = JsonConvert.DeserializeObject<JsonTeamImportDto[]>(jsonString);

            var validTeams = new List<Team>();

            foreach (var jsonTeam in teams)
            {
                if(!IsValid(jsonTeam) || jsonTeam.Trophies <= 0)
                {
                    output.AppendLine(ErrorMessage);
                    continue;
                }


                var ourTeam = new Team()
                {
                    Name = jsonTeam.Name,
                    Nationality = jsonTeam.Nationality,
                    Trophies = jsonTeam.Trophies
                };

                foreach (var jsonFootballer in jsonTeam.Footballers.Distinct())
                {
                    var findFoootballer = context.Footballers.Find(jsonFootballer);

                    if (findFoootballer == null)
                    {
                        output.AppendLine(ErrorMessage);
                        continue;
                    }


                    var ourFootballer = context.Footballers.FirstOrDefault(x => x.Id == jsonFootballer)
                        ?? new Footballer { Id = jsonFootballer };

                    ourTeam.TeamsFootballers.Add(new TeamFootballer { FootballerId = ourFootballer.Id });

                    /*ourTeam.TeamsFootballers.Add(new TeamFootballer()
                    {
                        Team = ourTeam,
                        FootballerId = jsonFootballer.FootballerId
                    });*/
                }

                validTeams.Add(ourTeam);
                output.AppendLine($"Successfully imported team - {ourTeam.Name}" +
                    $" with {ourTeam.TeamsFootballers.Count()} footballers.");
            }
            context.Teams.AddRange(validTeams);
            context.SaveChanges();

            return output.ToString().TrimEnd();
        }

        private static bool IsValid(object dto)
        {
            var validationContext = new ValidationContext(dto);
            var validationResult = new List<ValidationResult>();

            return Validator.TryValidateObject(dto, validationContext, validationResult, true);
        }
    }
}
