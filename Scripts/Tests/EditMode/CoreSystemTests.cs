using NUnit.Framework;
using RavenDeckbuilding.Core;
using UnityEngine;

namespace RavenDeckbuilding.Tests
{
    public class CoreSystemTests
    {
        [Test]
        public void GameContext_CreateWithValidCaster_ShouldBeValid()
        {
            // Arrange
            var testObject = new GameObject("TestPlayer");
            var mockPlayer = testObject.AddComponent<Player>();
            var targetPosition = Vector3.zero;
            
            // Act
            var context = GameContext.Create(mockPlayer, targetPosition);
            
            // Assert
            Assert.IsTrue(context.IsValid);
            Assert.AreEqual(mockPlayer, context.Caster);
            Assert.AreEqual(targetPosition, context.TargetPosition);
            
            // Cleanup
            Object.DestroyImmediate(testObject);
        }
        
        [Test]
        public void InputEvent_CreateWithValidData_ShouldHaveCorrectValues()
        {
            // Arrange
            var inputType = InputType.CardSelect;
            var position = new Vector2(100, 200);
            var cardIndex = 5;
            
            // Act
            var inputEvent = InputEvent.Create(inputType, position, cardIndex);
            
            // Assert
            Assert.AreEqual(inputType, inputEvent.InputType);
            Assert.AreEqual(position, inputEvent.Position);
            Assert.AreEqual(cardIndex, inputEvent.CardIndex);
            Assert.Greater(inputEvent.SequenceId, 0);
        }
        
        [Test]
        public void InputEvent_IsExpired_ShouldReturnCorrectValue()
        {
            // Arrange
            var inputEvent = InputEvent.Create(InputType.CardDrag, Vector2.zero);
            
            // Act & Assert
            Assert.IsFalse(inputEvent.IsExpired(1f)); // Should not be expired within 1 second
            Assert.IsTrue(inputEvent.IsExpired(-1f)); // Should be expired with negative max age
        }
        
        [Test]
        public void InputType_EnumValues_ShouldBeCorrect()
        {
            // Test that all expected enum values exist
            Assert.IsTrue(System.Enum.IsDefined(typeof(InputType), InputType.None));
            Assert.IsTrue(System.Enum.IsDefined(typeof(InputType), InputType.CardSelect));
            Assert.IsTrue(System.Enum.IsDefined(typeof(InputType), InputType.CardDrag));
            Assert.IsTrue(System.Enum.IsDefined(typeof(InputType), InputType.CardDrop));
            Assert.IsTrue(System.Enum.IsDefined(typeof(InputType), InputType.TargetSelect));
            Assert.IsTrue(System.Enum.IsDefined(typeof(InputType), InputType.Cancel));
            Assert.IsTrue(System.Enum.IsDefined(typeof(InputType), InputType.Confirm));
        }
    }
}