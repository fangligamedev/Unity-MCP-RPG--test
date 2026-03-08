#nullable enable
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace NineKingsPrototype.Tests.PlayMode
{
    public sealed class NineKingsPrototypePlayModeTests
    {
        private GameObject? _root;
        private NineKingsGameController? _controller;
        private NineKingsRuntimeUI? _runtimeUi;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _root = new GameObject("NineKingsPrototypeTestRoot");
            _controller = _root.AddComponent<NineKingsGameController>();
            yield return null;
            _runtimeUi = _root.GetComponent<NineKingsRuntimeUI>();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_root != null)
            {
                Object.Destroy(_root);
            }

            var camera = Camera.main;
            if (camera != null)
            {
                Object.Destroy(camera.gameObject);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator DebugStartNewRun_InitializesExpectedState()
        {
            Assert.That(_controller, Is.Not.Null);

            _controller!.DebugStartNewRun();
            yield return null;

            Assert.That(_controller.Year, Is.EqualTo(1));
            Assert.That(_controller.Lives, Is.EqualTo(3));
            Assert.That(_controller.HandCount, Is.EqualTo(4));
            Assert.That(_controller.CurrentEnemyKingId, Is.Not.Empty);
            Assert.That(_controller.CurrentPhase, Is.EqualTo(NKRunPhase.CardPlay));
        }

        [UnityTest]
        public IEnumerator GmCommands_CanMutateCoreRunValues()
        {
            Assert.That(_controller, Is.Not.Null);

            _controller!.DebugStartNewRun();
            yield return null;

            _controller.ExecuteGmCommand("set_gold 77");
            _controller.ExecuteGmCommand("set_lives 5");
            _controller.ExecuteGmCommand("set_year 33");
            yield return null;

            Assert.That(_controller.Gold, Is.EqualTo(77));
            Assert.That(_controller.Lives, Is.EqualTo(5));
            Assert.That(_controller.Year, Is.EqualTo(33));
        }

        [UnityTest]
        public IEnumerator GmPanel_WhenShown_RemainsInsideScreenBounds()
        {
            Assert.That(_controller, Is.Not.Null);
            Assert.That(_runtimeUi, Is.Not.Null);

            _controller!.DebugStartNewRun();
            yield return null;

            _runtimeUi!.ToggleGM(true);
            yield return null;

            var gmObject = GameObject.Find("GMRoot");
            Assert.That(gmObject, Is.Not.Null, "GMRoot 未创建。");

            var rect = gmObject!.GetComponent<RectTransform>();
            Assert.That(rect, Is.Not.Null, "GMRoot 缺少 RectTransform。");

            var corners = new Vector3[4];
            rect!.GetWorldCorners(corners);

            Assert.That(corners[0].x, Is.GreaterThanOrEqualTo(-1f), "GM 面板左侧超出屏幕。");
            Assert.That(corners[0].y, Is.GreaterThanOrEqualTo(-1f), "GM 面板底部超出屏幕。");
            Assert.That(corners[2].x, Is.LessThanOrEqualTo(Screen.width + 1f), "GM 面板右侧超出屏幕。");
            Assert.That(corners[2].y, Is.LessThanOrEqualTo(Screen.height + 1f), "GM 面板顶部超出屏幕。");
        }
    }
}