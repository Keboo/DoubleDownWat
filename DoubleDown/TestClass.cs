using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DoubleDown
{
    [TestClass]
    public class TestClass
    {
        [TestMethod]
        public void Test()
        {
            var dd = new DoubleDown();

            int invocationCount = dd.Get<int>();
            
            Assert.AreEqual(2, invocationCount);
        }
    }

    public class DoubleDown
    {
        public T Get<T>()
        {
            if (GetInternal() is T result)
            {
                return result;
            }
            return default(T);
        }

        private int _invokeCount;
        private object GetInternal()
        {
            return ++_invokeCount;
        }
    }
}
