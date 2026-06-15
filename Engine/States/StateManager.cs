using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MyGame.Engine.States;

public class StateManager
{
    private readonly List<GameState> stateStack = new();
    private readonly List<Action> pendingOperations = new();

    // ARCHITECTURE FIX: Prevents visual "flash" by blocking updates/draws during transitions
    public bool IsTransitioning { get; private set; } = false;

    public static StateManager Instance { get; private set; } = null!;

    public StateManager()
    {
       Instance = this;
    }

    public void PushState(GameState state)
    {
       pendingOperations.Add(() =>
       {
          stateStack.Add(state);
          state.LoadContent();
       });
    }

    public void PopState()
    {
       pendingOperations.Add(() =>
       {
          if (stateStack.Count > 0)
          {
             stateStack[^1].UnloadContent();
             stateStack.RemoveAt(stateStack.Count - 1);
          }
       });
    }

    public void ChangeState(GameState state)
    {
       // ARCHITECTURE FIX: Block incoming input/update logic during transition
       IsTransitioning = true;
       pendingOperations.Clear();

       pendingOperations.Add(() =>
       {
          foreach (var existingState in stateStack)
          {
             existingState.UnloadContent();
          }

          stateStack.Clear();
          stateStack.Add(state);
          state.LoadContent();

          // Re-enable rendering
          IsTransitioning = false;
       });
    }

    // ARCHITECTURE FIX: Accept a direct delta time step float to separate loop ticking from engine internals
    public void Update(float deltaTime)
    {
       foreach (var op in pendingOperations) op();
       pendingOperations.Clear();

       if (IsTransitioning || stateStack.Count == 0) return;

       // Securely reconstruct an isolated read-only wrapper context to push downstream to existing systems safely
       var stateGameTime = new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(deltaTime));
       stateStack[^1].Update(stateGameTime);
    }

    public void Draw(SpriteBatch spriteBatch)
    {
       if (IsTransitioning || stateStack.Count == 0) return;
       foreach (var state in stateStack)
       {
          state.Draw(spriteBatch);
       }
    }
}
