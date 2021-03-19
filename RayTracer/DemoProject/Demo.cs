using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using RayTracer;
using System;

namespace DemoProject
{
    public class Demo : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;

        private Texture2D PixelTexture;

        private System.Numerics.Vector2 StartPosition = new System.Numerics.Vector2(0);
        private System.Numerics.Vector2 EndPosition = new System.Numerics.Vector2(0);

        private readonly Tile[,] TileMap = new Tile[32, 32];
        private readonly int TileSize = 16;

        private KeyboardState KeyboardState;
        private MouseState MouseState;
        private MouseState OldMouseState;

        public class Tile : IRayTraceable
        {
            public bool Solid { get; set; }
        }

        public System.Numerics.Vector2? CollisionPoint;

        public Demo()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            OldMouseState = Mouse.GetState();

			TargetElapsedTime = TimeSpan.FromMilliseconds(1000d / 240d);
			IsFixedTimeStep = true;
			_graphics.SynchronizeWithVerticalRetrace = false;

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            PixelTexture = new Texture2D(GraphicsDevice, 1, 1);
            PixelTexture.SetData(new Color[1] { Color.White });

			for(int x = 0; x < TileMap.GetLength(0); x++)
            {
				for(int y = 0; y < TileMap.GetLength(0); y++)
                {
					TileMap[x, y] = new Tile();
                }
            }
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            KeyboardState = Keyboard.GetState();
            MouseState = Mouse.GetState();

            if (MouseState.RightButton == ButtonState.Pressed)
            {
                Point truncatedMouse = new Point(MouseState.X / TileSize, MouseState.Y / TileSize);

                if (truncatedMouse.X >= 0 && truncatedMouse.X < TileMap.GetLength(0)
                    && truncatedMouse.Y >= 0 && truncatedMouse.Y < TileMap.GetLength(1))
                {
                    // Flip the given tiles value.
                    TileMap[truncatedMouse.X, truncatedMouse.Y].Solid = true;
                }
            }

            if (KeyboardState.IsKeyDown(Keys.W)) StartPosition.Y -= 25f * (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (KeyboardState.IsKeyDown(Keys.S)) StartPosition.Y += 25f * (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (KeyboardState.IsKeyDown(Keys.A)) StartPosition.X -= 25f * (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (KeyboardState.IsKeyDown(Keys.D)) StartPosition.X += 25f * (float)gameTime.ElapsedGameTime.TotalSeconds;

			EndPosition = new System.Numerics.Vector2(MouseState.Position.X, MouseState.Position.Y);

            CollisionPoint = DDARayTracer.Cast(new System.Numerics.Vector2(StartPosition.X, StartPosition.Y), new System.Numerics.Vector2(EndPosition.X / TileSize, EndPosition.Y / TileSize), TileMap);

            OldMouseState = MouseState;

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);

            _spriteBatch.Begin();

            for (int x = 0; x < TileMap.GetLength(0); x++)
            {
                for (int y = 0; y < TileMap.GetLength(1); y++)
                {
                    Color tileColor = TileMap[x, y].Solid ? Color.Blue : Color.Black;

                    _spriteBatch.Draw(PixelTexture, new Rectangle(x * TileSize, y * TileSize, TileSize, TileSize), tileColor);
                }
            }

            // Draw start position
            _spriteBatch.Draw(PixelTexture, new Rectangle(new Vector2(StartPosition.X * TileSize, StartPosition.Y * TileSize).ToPoint() - new Point(8), new Point(16)), null, Color.Green, 0f, Vector2.Zero, SpriteEffects.None, 1f);

            // Draw end position
            _spriteBatch.Draw(PixelTexture, new Rectangle(new Vector2(EndPosition.X, EndPosition.Y).ToPoint() - new Point(8), new Point(16)), null, Color.Red, 0f, Vector2.Zero, SpriteEffects.None, 1f);

			if (MouseState.LeftButton == ButtonState.Pressed)
			{
				DrawLine(_spriteBatch, new Vector2(StartPosition.X * TileSize, StartPosition.Y * TileSize), new Vector2(EndPosition.X, EndPosition.Y), Color.White);

				if (CollisionPoint != null)
				{
					_spriteBatch.Draw(PixelTexture, new Rectangle(new Vector2(CollisionPoint.Value.X * TileSize, CollisionPoint.Value.Y * TileSize).ToPoint() - new Point(2), new Point(4)), null, Color.Yellow, 0f, Vector2.Zero, SpriteEffects.None, 1f);
				}
			}

            _spriteBatch.End();

            base.Draw(gameTime);
        }

		public void DrawLine(SpriteBatch spriteBatch, Vector2 start, Vector2 end, Color color)
        {
            float angle = (float)Math.Atan2(end.Y - start.Y, end.X - start.X);

            float distance = Vector2.Distance(start, end);

            spriteBatch.Draw(PixelTexture, start, null, color, angle, Vector2.Zero, new Vector2(distance, 1f), SpriteEffects.None, 1f);
        }
    }
}
