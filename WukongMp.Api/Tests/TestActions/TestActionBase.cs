namespace WukongMp.Api.Tests.TestActions
{
    public abstract class TestActionBase
    {
        public string Description { get; set; } = "";
        protected float Timeout { get; set; } = 10; // seconds

        protected float ElapsedTime = 0; // seconds

        public abstract TestState Update(float deltaTime);
    }
}
