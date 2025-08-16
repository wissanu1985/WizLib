using System;
using Shouldly;
using Wiz.Utility.Extensions;
using Xunit;

namespace Wiz.Utility.Test.Extensions
{
    public class MappingErrorTests
    {
        private sealed class Src
        {
            public string? A { get; set; }
        }

        private sealed class Dest
        {
            public int A { get; set; }
        }

        [Fact]
        public void ErrorHandler_IsCalled_OnInvalidSimpleConversion_WithThrow()
        {
            // Arrange
            var src = new Src { A = "not-a-number" };
            MappingError? captured = null;
            var options = new MappingOptions
            {
                ConversionFailure = ConversionFailureBehavior.Throw,
                ErrorHandler = me => captured = me
            };

            // Act
            var ex = Should.Throw<InvalidCastException>(() => src.Adapt<Dest>(options));

            // Assert
            captured.ShouldNotBeNull();
            captured.Value.SourceType.ShouldBe(typeof(Src));
            captured.Value.DestinationType.ShouldBe(typeof(Dest));
            captured.Value.DestinationMember.ShouldBe("A");
            captured.Value.Exception.ShouldBeOfType<InvalidCastException>();
            ex.Message.ShouldContain("Cannot convert value of type");
        }

        [Fact]
        public void MappingError_Record_CanBeConstructed_AndHasProperties()
        {
            // Arrange
            var inner = new InvalidOperationException("boom");
            var me = new MappingError(typeof(string), typeof(int), "Value", inner);

            // Assert
            me.SourceType.ShouldBe(typeof(string));
            me.DestinationType.ShouldBe(typeof(int));
            me.DestinationMember.ShouldBe("Value");
            me.Exception.ShouldBe(inner);
        }
    }
}
