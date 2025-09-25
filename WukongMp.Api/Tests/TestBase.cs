namespace WukongMp.Api.Tests
{
    internal abstract class TestBase
    {
        public string Description { get; set; } = "";
        protected float Timeout { get; set; } = 10; // seconds

        protected float ElapsedTime = 0; // seconds

        public abstract TestState Update(float deltaTime);
    }
}
