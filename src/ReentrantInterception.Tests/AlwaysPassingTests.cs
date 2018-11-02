using System;
using Xunit;

namespace ReentrantInterception.Tests
{
    public class AlwaysPassingTests
    {
        [Fact]
        public void GuaranteedSuccess()
        {
            Assert.True(true);
        }
    }
}
