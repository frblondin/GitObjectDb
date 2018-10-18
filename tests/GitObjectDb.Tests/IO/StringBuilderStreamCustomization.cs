using AutoFixture;
using System.Linq;
using System.Text;

namespace GitObjectDb.Tests.IO
{
    public class StringBuilderStreamCustomization : ICustomization
    {
        public void Customize(IFixture fixture)
        {
            var stringBuilder = new StringBuilder();
            var @string = fixture.Create<string>();
            foreach (var i in Enumerable.Range(0, 1000))
            {
                stringBuilder.Append(@string);
            }

            fixture.Inject(stringBuilder);
        }
    }
}
