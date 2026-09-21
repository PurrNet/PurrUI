using System.Collections;
using System.Text.RegularExpressions;
using NUnit.Framework;
using PurrNet.UI;
using UnityEngine;
using UnityEngine.TestTools;

namespace PurrNet.UI.Tests
{
    public class TestViewA : MonoView { }

    public class TestViewB : MonoView { }

    public class ViewStackTests
    {
        private GameObject _stackGo;
        private ViewStack _stack;
        private TestViewA _templateA;
        private TestViewB _templateB;

        [SetUp]
        public void SetUp()
        {
            _stackGo = new GameObject("ViewStack");
            _stack = _stackGo.AddComponent<ViewStack>();
            _templateA = new GameObject("TemplateA").AddComponent<TestViewA>();
            _templateB = new GameObject("TemplateB").AddComponent<TestViewB>();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var view in Object.FindObjectsByType<MonoView>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (view)
                    Object.DestroyImmediate(view.gameObject);
            }

            if (_stackGo)
                Object.DestroyImmediate(_stackGo);
        }

        [Test]
        public void Push_AddsViewToStack()
        {
            var view = _stack.Push(_templateA);

            Assert.IsNotNull(view);
            Assert.AreEqual(1, _stack.count);
            Assert.AreSame(view, _stack.top);
            Assert.AreSame(_stack, view.parentStack);
            Assert.IsTrue(view.isInStack);
            Assert.IsTrue(view.isTopMost);
        }

        [Test]
        public void Push_Second_SendsFirstToBackground()
        {
            var first = _stack.Push(_templateA);
            var second = _stack.Push(_templateB);

            Assert.AreEqual(2, _stack.count);
            Assert.AreSame(second, _stack.top);
            Assert.IsFalse(first.isTopMost);
            Assert.IsFalse(first.canvasGroup.interactable);
            Assert.IsFalse(first.canvasGroup.blocksRaycasts);
        }

        [Test]
        public void Push_NullPrefab_LogsErrorAndReturnsNull()
        {
            LogAssert.Expect(LogType.Error, new Regex("prefab is null"));
            Assert.IsNull(_stack.Push<MonoView>(null));
            Assert.AreEqual(0, _stack.count);
        }

        [UnityTest]
        public IEnumerator Pop_RemovesAndDestroysView()
        {
            var view = _stack.Push(_templateA);
            _stack.Pop();

            Assert.AreEqual(0, _stack.count);
            Assert.IsFalse(view.isInStack);

            yield return null;
            Assert.IsTrue(view == null);
        }

        [Test]
        public void Pop_EmptyStack_LogsError()
        {
            LogAssert.Expect(LogType.Error, new Regex("No windows to pop"));
            _stack.Pop();
        }

        [UnityTest]
        public IEnumerator Pop_RestoresPreviousTopInteractivity()
        {
            var first = _stack.Push(_templateA);
            _stack.Push(_templateB);
            _stack.Pop();

            Assert.AreSame(first, _stack.top);
            Assert.IsTrue(first.canvasGroup.interactable);
            Assert.IsTrue(first.canvasGroup.blocksRaycasts);
            yield break;
        }

        [UnityTest]
        public IEnumerator PopInstance_RemovesMiddleView()
        {
            var a = _stack.Push(_templateA);
            var b = _stack.Push(_templateB);
            var c = _stack.Push(_templateA);

            _stack.Pop(b);

            Assert.AreEqual(2, _stack.count);
            Assert.AreSame(c, _stack.top);

            yield return null;
            Assert.IsTrue(b == null);
            Assert.IsTrue(a != null);
            Assert.IsTrue(c != null);
        }

        [UnityTest]
        public IEnumerator Clear_DestroysAllViews()
        {
            var a = _stack.Push(_templateA);
            var b = _stack.Push(_templateB);
            var c = _stack.Push(_templateA);

            _stack.Clear();

            Assert.AreEqual(0, _stack.count);

            yield return null;
            Assert.IsTrue(a == null);
            Assert.IsTrue(b == null);
            Assert.IsTrue(c == null);
        }

        [UnityTest]
        public IEnumerator Replace_SwapsTopView()
        {
            var a = _stack.Push(_templateA);
            var b = _stack.Replace(_templateB);

            Assert.AreEqual(1, _stack.count);
            Assert.AreSame(b, _stack.top);

            yield return null;
            Assert.IsTrue(a == null);
        }

        [Test]
        public void MoveToTop_ReordersStack()
        {
            var a = _stack.Push(_templateA);
            var b = _stack.Push(_templateB);

            Assert.IsTrue(_stack.MoveToTop(a));
            Assert.AreSame(a, _stack.top);
            Assert.AreEqual(2, _stack.count);

            Assert.IsFalse(_stack.MoveToTop(a));
            Assert.IsTrue(b != null);
        }

        [Test]
        public void FindWindow_SearchesTopDown()
        {
            var a = _stack.Push(_templateA);
            var b = _stack.Push(_templateB);
            var c = _stack.Push(_templateA);

            Assert.AreSame(c, _stack.FindWindow<TestViewA>());
            Assert.AreSame(a, _stack.GetFirstView<TestViewA>());
            Assert.AreSame(b, _stack.FindWindow<TestViewB>());
            Assert.IsTrue(_stack.Contains<TestViewB>());
        }

        [Test]
        public void Views_ExposesStackBottomToTop()
        {
            var a = _stack.Push(_templateA);
            var b = _stack.Push(_templateB);

            Assert.AreEqual(2, _stack.views.Count);
            Assert.AreSame(a, _stack.views[0]);
            Assert.AreSame(b, _stack.views[1]);
        }

        [Test]
        public void Events_RaisedOnPushAndPop()
        {
            MonoView pushed = null;
            MonoView popped = null;
            _stack.onViewPushed += v => pushed = v;
            _stack.onViewPopped += v => popped = v;

            var view = _stack.Push(_templateA);
            Assert.AreSame(view, pushed);

            _stack.Pop();
            Assert.AreSame(view, popped);
        }

        [UnityTest]
        public IEnumerator PopMe_PopsViewFromItsStack()
        {
            var view = _stack.Push(_templateA);
            view.PopMe();

            Assert.AreEqual(0, _stack.count);

            yield return null;
            Assert.IsTrue(view == null);
        }

        [Test]
        public void PopMe_NotInStack_LogsError()
        {
            LogAssert.Expect(LogType.Error, new Regex("not part of a ViewStack"));
            _templateA.PopMe();
        }
    }
}
