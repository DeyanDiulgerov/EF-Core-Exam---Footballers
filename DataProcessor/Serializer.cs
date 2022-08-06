namespace Footballers.DataProcessor
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Xml.Serialization;
    using Data;
    using Footballers.DataProcessor.ExportDto;
    using Newtonsoft.Json;
    using Formatting = Newtonsoft.Json.Formatting;

    public class Serializer
    {
        public static string ExportCoachesWithTheirFootballers(FootballersContext context)
        {
            /*Export all coaches that train at least one footballer.
                         For each coach, export their name and footballers count.
                      For each footballer, export their name and position type.
                    Order the footballers by name(ascending).
                   Order the coaches by footballers count(descending), then by name(ascending).
            NOTE: You may need to call.ToArray() function before the selection,
                     in order to detach entities from the database and avoid runtime errors(EF Core bug).*/
            StringBuilder output = new StringBuilder();

            XmlSerializer xmlSerializer = new XmlSerializer
                (typeof(ExportXmlCoachDto[]), new XmlRootAttribute("Coaches"));

            XmlSerializerNamespaces namespaces = new XmlSerializerNamespaces();
            namespaces.Add(string.Empty, string.Empty);

            using StringWriter sw = new StringWriter(output);


            var coaches = context.Coaches
                .Where(x => x.Footballers.Count() > 0)
                .ToArray()
                .Select(x => new ExportXmlCoachDto
                {
                    Name = x.Name,
                    FootballersCount = x.Footballers.Count(),
                    Footballers = x.Footballers
                        .ToArray()
                        .Select(f => new ExportXmlCoachFootballerDto()
                        {
                            Name = f.Name,
                            Position = f.PositionType.ToString()
                        })
                        .OrderBy(f => f.Name)
                        .ToArray()
                })
                .OrderByDescending(x => x.Footballers.Count())
                .ThenBy(x => x.Name)
                .ToArray();

            xmlSerializer.Serialize(sw, coaches, namespaces);

            return output.ToString().TrimEnd();
        }

        public static string ExportTeamsWithMostFootballers(FootballersContext context, DateTime date)
        {
/*Select the top 5 teams that have at least one footballer that their contract start date is higher
           or equal to the given date.
 Select them with their footballers who meet the same criteria
               (their contract start date is after or equals the given date).
       For each team, export their name and their footballers.
For each footballer, export their name and contract start date(must be in format "d"),
         contract end date(must be in format "d"), position and best skill type.
     Order the footballers by contract end date(descending),
       then by name(ascending).
    Order the teams by all footballers(meeting above condition) count(descending),
         then by name(ascending).
NOTE: Do not forget to use CultureInfo.InvariantCulture.You may need to call.ToArray() function before the
       selection in order to detach entities from the database and avoid runtime errors(EF Core bug).*/
            var teams = context.Teams
                .Where(t => t.TeamsFootballers.Any(tf => tf.Footballer.ContractEndDate >= date))
                .ToArray()
                .Select(t => new
                {
                    Name = t.Name,
                    Footballers = t.TeamsFootballers
                        .Where(tf => tf.Footballer.ContractStartDate >= date)
                        .OrderByDescending(tf => tf.Footballer.ContractEndDate)
                        .ThenBy(tf => tf.Footballer.Name)
                        .Select(tf => new
                        {
                            FootballerName = tf.Footballer.Name,
                            ContractStartDate = tf.Footballer.ContractStartDate.ToString("d", CultureInfo.InvariantCulture),
                            ContractEndDate = tf.Footballer.ContractEndDate.ToString("d", CultureInfo.InvariantCulture),
                            BestSkillType = tf.Footballer.BestSkillType.ToString(),
                            PositionType = tf.Footballer.PositionType.ToString()
                        })
                        .ToArray()
                })
                .ToArray()
                .OrderByDescending(t => t.Footballers.Count())
                .ThenBy(t => t.Name)
                .Take(5)
                .ToArray();

            return JsonConvert.SerializeObject(teams, Formatting.Indented);
        }
    }
}
