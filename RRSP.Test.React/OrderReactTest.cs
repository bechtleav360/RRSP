using Signum.Authorization;

namespace RRSP.Test.React;

public class OrderReactTest : RRSPTestClass
{
    public OrderReactTest()
    {
        RRSPEnvironment.StartAndInitialize();
        AuthLogic.GloballyEnabled = false;
    }
}
