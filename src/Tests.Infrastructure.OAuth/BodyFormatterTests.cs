using Core.Infrastructure.OAuth;
using FluentAssertions;
using System.Collections.Generic;
using Xunit;

namespace Tests.Infrastructure.OAuth
{
    public class BodyFormatterTests
    {
        [Fact(DisplayName = "BF-001: Formatter should concat all non empty fields.")]
        public void BF001()
        {
            //  arange
            var fields = new Dictionary<string, string>()
            {
                { "one", "value-one" },
                { "two", "value-two" },
                { "three", null }
            };

            // act
            var body = BodyFormatter.Format(fields);

            // assert
            body.Should().Be("one=value-one&two=value-two");
        }
    }
}