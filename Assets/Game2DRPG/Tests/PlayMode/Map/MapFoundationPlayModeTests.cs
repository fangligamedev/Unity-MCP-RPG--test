#nullable enable
/*
 * Copyright (c) 2026.
 */

using System.Collections;
using Game2DRPG.Map.Runtime;
using Game2DRPG.Runtime;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game2DRPG.Map.PlayMode.Tests
{
    public sealed class MapFoundationPlayModeTests
    {
        [UnityTest]
        public IEnumerator AnimationActivationService_CanBeConfigured()
        {
            var serviceObject = new GameObject("AnimationService");
            var service = serviceObject.AddComponent<AnimationActivationService>();

            var playerObject = new GameObject("Player");
            playerObject.AddComponent<Rigidbody2D>();
            playerObject.AddComponent<SpriteRenderer>();
            var player = playerObject.AddComponent<TopDownPlayerController>();

            var cameraObject = new GameObject("Camera");
            var camera = cameraObject.AddComponent<Camera>();

            service.Configure(MapMode.RoomChain, player, camera);
            yield return null;

            Assert.That(service.MapMode, Is.EqualTo(MapMode.RoomChain));

            Object.DestroyImmediate(serviceObject);
            Object.DestroyImmediate(playerObject);
            Object.DestroyImmediate(cameraObject);
        }

        [UnityTest]
        public IEnumerator AnimationActivationService_TogglesAnimatorTargetsByCameraProximity()
        {
            var serviceObject = new GameObject("AnimationService");
            var service = serviceObject.AddComponent<AnimationActivationService>();

            var playerObject = new GameObject("Player");
            playerObject.AddComponent<Rigidbody2D>();
            playerObject.AddComponent<SpriteRenderer>();
            var player = playerObject.AddComponent<TopDownPlayerController>();

            var cameraObject = new GameObject("Camera");
            cameraObject.transform.position = Vector3.zero;
            var camera = cameraObject.AddComponent<Camera>();

            var animatedObject = new GameObject("AnimatedTarget");
            animatedObject.transform.position = new Vector3(1f, 0f, 0f);
            var renderer = animatedObject.AddComponent<SpriteRenderer>();
            renderer.sprite = CreateTestSprite();
            var animator = animatedObject.AddComponent<Animator>();
            var target = animatedObject.AddComponent<AnimationActivationTarget>();
            target.Configure(AnimationChannel.AnimatedVegetation, ActivationPolicy.ByCameraProximity, "combat_room", string.Empty, 3f);
            target.SetRuntimeActive(false);

            service.Configure(MapMode.RoomChain, player, camera);
            yield return null;

            Assert.That(renderer.enabled, Is.True);
            Assert.That(animator.enabled, Is.True);

            animatedObject.transform.position = new Vector3(10f, 0f, 0f);
            yield return null;

            Assert.That(renderer.enabled, Is.False);
            Assert.That(animator.enabled, Is.False);

            Object.DestroyImmediate(animatedObject);
            Object.DestroyImmediate(serviceObject);
            Object.DestroyImmediate(playerObject);
            Object.DestroyImmediate(cameraObject);
        }

        [UnityTest]
        public IEnumerator AnimationActivationService_KeepsAlwaysOnSpritePlayerActive()
        {
            var serviceObject = new GameObject("AnimationService");
            var service = serviceObject.AddComponent<AnimationActivationService>();

            var playerObject = new GameObject("Player");
            playerObject.AddComponent<Rigidbody2D>();
            playerObject.AddComponent<SpriteRenderer>();
            var player = playerObject.AddComponent<TopDownPlayerController>();

            var cameraObject = new GameObject("Camera");
            cameraObject.transform.position = Vector3.zero;
            var camera = cameraObject.AddComponent<Camera>();

            var animatedObject = new GameObject("WaterFoam");
            var renderer = animatedObject.AddComponent<SpriteRenderer>();
            renderer.sprite = CreateTestSprite();
            var playerComponent = animatedObject.AddComponent<AnimatedSpritePlayer>();
            playerComponent.Configure(new[] { renderer.sprite }, 4f, true, false);
            var target = animatedObject.AddComponent<AnimationActivationTarget>();
            target.Configure(AnimationChannel.AnimatedShoreline, ActivationPolicy.AlwaysOn, string.Empty, string.Empty, 4f);
            target.SetRuntimeActive(false);

            service.Configure(MapMode.RoomChain, player, camera);
            yield return null;

            Assert.That(renderer.enabled, Is.True);
            Assert.That(playerComponent.IsPlaying, Is.True);

            Object.DestroyImmediate(animatedObject);
            Object.DestroyImmediate(serviceObject);
            Object.DestroyImmediate(playerObject);
            Object.DestroyImmediate(cameraObject);
        }

        private static Sprite CreateTestSprite()
        {
            var texture = new Texture2D(8, 8, TextureFormat.RGBA32, false);
            var pixels = new Color[64];
            for (var index = 0; index < pixels.Length; index++)
            {
                pixels[index] = Color.white;
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, 8f, 8f), new Vector2(0.5f, 0.5f), 8f);
        }
    }
}
