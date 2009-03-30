using System;
using System.IO;
using System.Collections.Generic;
using System.Collections;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Net;
using Microsoft.Xna.Framework.Storage;
using XnaConsole;

namespace MPQNav
{
    /// <summary>
    /// Struct outlining our custom vertex
    /// </summary>
    public struct VertexPositionNormalColored
    {
        /// <summary>
        /// Vector3 Position for this vertex
        /// </summary>
        public Vector3 Position;
        /// <summary>
        /// Color of this vertex
        /// </summary>
        public Color Color;
        /// <summary>
        /// Normal vector for this Vertex
        /// </summary>
        public Vector3 Normal;

        /// <summary>
        /// Constructor for a VertexPositionNormalColored
        /// </summary>
        /// <param name="position">Vector3 Position of the vertex</param>
        /// <param name="color">Color of the vertex</param>
        /// <param name="normal">Normal vector of the vertex</param>
        public VertexPositionNormalColored(Vector3 position, Color color, Vector3 normal)
        {
            this.Position = position;
            this.Color = color;
            this.Normal = normal;
        }
        /// <summary>
        /// Memory size for a VertexPositionNormalColored
        /// </summary>
        public static int SizeInBytes = 7 * 4;
        /// <summary>
        /// VertexElement array (used for rendering)
        /// </summary>
        public static VertexElement[] VertexElements = new VertexElement[]
        {
            new VertexElement( 0, 0, VertexElementFormat.Vector3, VertexElementMethod.Default, VertexElementUsage.Position, 0 ),
            new VertexElement( 0, sizeof(float) * 3, VertexElementFormat.Color, VertexElementMethod.Default, VertexElementUsage.Color, 0 ),
            new VertexElement( 0, sizeof(float) * 4, VertexElementFormat.Vector3, VertexElementMethod.Default, VertexElementUsage.Normal, 0 ),
        };
    }

    /// <summary>
    /// This is the class that controls the entire game.
    /// </summary>
    public class Game1 : Microsoft.Xna.Framework.Game
    {
        // XNA uses this variable for graphics information
        GraphicsDeviceManager graphics;
        // We use this to declare our verticies
        VertexDeclaration vertexDeclaration;
        // Another set of XNA Variables
        Matrix view;
        Matrix proj;

        BasicEffect basicEffect;

        ADT.ADTManager manager;
        /// <summary>
        /// Console used to execute commands while the game is running.
        /// </summary>
        public MpqConsole console;

        // Camera Stuff
        Vector3 avatarPosition = new Vector3(-100, 100, -100);
        Vector3 avatarHeadOffset = new Vector3(0, 10, 0);
        float avatarYaw;
        Vector3 cameraReference = new Vector3(0, 0, 10);
        Vector3 thirdPersonReference = new Vector3(0, 20, -20);
        float rotationSpeed = 1f / 60f;
        float forwardSpeed = 50f / 60f;
        static float viewAngle = MathHelper.PiOver4;
        static float nearClip = 1.0f;
        static float farClip = 2000.0f;

        KeyboardState oldKeyState;

        private List<VertexPositionNormalColored> renderVerticies = new List<VertexPositionNormalColored>();
        private List<int> renderIndices = new List<int>();

        SpriteBatch spriteBatch;
        SpriteFont spriteFont;

        /// <summary>
        /// Constructor for the game.
        /// </summary>
        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            
            
            String mpqPath = System.Configuration.ConfigurationSettings.AppSettings["mpqPath"];
            //String mpqFile = "c:\\Program Files\\World of Warcraft\\Data\\common.MPQ";
            String defaultContinent = System.Configuration.ConfigurationSettings.AppSettings["defaultContinent"];
            int defaultMapX = int.Parse(System.Configuration.ConfigurationSettings.AppSettings["defaultMapX"]);
            int defaultMapY = int.Parse(System.Configuration.ConfigurationSettings.AppSettings["defaultMapY"]);

            MPQNav.ADT.ADTManager.continent_type continent = (MPQNav.ADT.ADTManager.continent_type)Enum.Parse(typeof(MPQNav.ADT.ADTManager.continent_type), defaultContinent, true);

            manager = new MPQNav.ADT.ADTManager(continent, mpqPath);

            manager.loadADT(defaultMapX, defaultMapY);

            this.renderIndices = manager.renderingIndices();
            this.renderVerticies = manager.renderingVerticies();
            if (this.renderIndices.Count > 0)
            {
                this.avatarPosition = renderVerticies[0].Position;
                this.avatarYaw = 90;
            }
        }

        /// <summary>
        /// Executes a console command.
        /// </summary>
        public void DoCommand()
        {
            if (console.Command.commandCode.Equals(MpqConsole.ConsoleCommandStruct.CommandCode.Load))
            {
                String command = console.Command.commandData;
                int map_x = int.Parse(command.Split(" ".ToCharArray())[0]);
                int map_y = int.Parse(command.Split(" ".ToCharArray())[1]);
                console.WriteLine("Loading map:" + map_x + " " + map_y);
                manager.loadADT(map_x, map_y);
            }
        }
        /// <summary>
        /// Loads the content needed for the game.
        /// </summary>
        protected override void LoadContent()
        {
            oldKeyState = Keyboard.GetState();
            SpriteFont thaFont = this.Content.Load<SpriteFont>(@"mfont");
            this.spriteFont = thaFont;
            console = new MpqConsole(this, thaFont);            
            console.MyEvent += new CommandDelegate(DoCommand);

            spriteBatch = new SpriteBatch(this.GraphicsDevice);
        }


        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            // TODO: Add your initialization logic here
            //this.ParseAdt();
            graphics.PreferredBackBufferWidth = 1024;
            graphics.PreferredBackBufferHeight = 768;
            graphics.IsFullScreen = false;
            graphics.ApplyChanges();
            this.InitializeEffect();
            base.Initialize();
        }

        private void InitializeEffect()
        {
            basicEffect = new BasicEffect(graphics.GraphicsDevice, null);
            basicEffect.VertexColorEnabled = true;
            basicEffect.View = view;
            basicEffect.Projection = proj;
            this.vertexDeclaration = new VertexDeclaration(graphics.GraphicsDevice, VertexPositionNormalColored.VertexElements);

        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            UpdateAvatarPosition();
            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            graphics.GraphicsDevice.Clear(Color.Black);
            graphics.GraphicsDevice.VertexDeclaration = vertexDeclaration;
            // Update our camera
            UpdateCameraThirdPerson();

            basicEffect.Begin();
            basicEffect.Projection = proj;
            basicEffect.View = view;
            
            basicEffect.Alpha = 1.0f;
            basicEffect.DiffuseColor = new Vector3(.75f, .75f, .75f);
            basicEffect.SpecularColor = new Vector3(0.25f, 0.25f, 0.25f);
            basicEffect.SpecularPower = 5.0f;

            basicEffect.AmbientLightColor = new Vector3(0.75f, 0.75f, 0.75f);
            basicEffect.DirectionalLight0.Enabled = true;
            basicEffect.DirectionalLight0.DiffuseColor = Vector3.One;
            basicEffect.DirectionalLight0.Direction = Vector3.Normalize(new Vector3(1.0f, -1.0f, -1.0f));
            basicEffect.DirectionalLight0.SpecularColor = Vector3.One;

            basicEffect.DirectionalLight1.Enabled = true;
            basicEffect.DirectionalLight1.DiffuseColor = new Vector3(0.8f, 0.8f, 0.8f);
            basicEffect.DirectionalLight1.Direction = Vector3.Normalize(new Vector3(-1.0f, -1.0f, 1.0f));

            basicEffect.LightingEnabled = true;
            
            foreach (EffectPass pass in basicEffect.CurrentTechnique.Passes)
            {
                pass.Begin();
                graphics.GraphicsDevice.RenderState.FillMode = FillMode.WireFrame;
                Render();
                graphics.GraphicsDevice.RenderState.FillMode = FillMode.Solid;
                pass.End();
            }
            basicEffect.End();
            Matrix World = Matrix.CreateRotationY(avatarYaw) * Matrix.CreateTranslation(avatarPosition);
            DrawCameraState();
            spriteBatch.Begin();
            spriteBatch.DrawString(spriteFont, "Esc: Opens the console.", new Vector2(10, graphics.GraphicsDevice.Viewport.Height - 30), Color.White);
            spriteBatch.End();
            base.Draw(gameTime);
        }

        private void Render()
        {
            this.renderIndices = manager.renderingIndices();
            this.renderVerticies = manager.renderingVerticies();
            if (this.renderIndices.Count > 0)
            {
                graphics.GraphicsDevice.DrawUserIndexedPrimitives<VertexPositionNormalColored>(
                        PrimitiveType.TriangleList,
                        renderVerticies.ToArray(),
                        0,   // vertex buffer offset to add to each element of the index buffer
                        renderVerticies.ToArray().Length,   // number of vertices to draw
                        renderIndices.ToArray(),
                        0,   // first index element to read
                        renderIndices.ToArray().Length / 3    // number of primitives to draw
                    );
            }
        }

        private void DrawCameraState()
        {
            graphics.GraphicsDevice.RenderState.CullMode = CullMode.None;
            graphics.GraphicsDevice.RenderState.DepthBufferEnable = true;
        }

        /// <summary>
        /// This is the method that is called to move our avatar 
        /// </summary>
        /// <code>
        /// // create the class that does translations
        /// GiveHelpTransforms ght = new GiveHelpTransforms();
        /// // have it load our XML into the SourceXML property
        /// ght.LoadXMLFromFile(
        ///      "E:\\Inetpub\\wwwroot\\GiveHelp\\GiveHelpDoc.xml");
        /// </code>
        void UpdateAvatarPosition()
        {
            KeyboardState keyboardState = Keyboard.GetState();
            GamePadState currentState = GamePad.GetState(PlayerIndex.One);
            MouseState mouseState = Mouse.GetState();

            if (!console.IsOpen())
            {
                if(keyboardState.IsKeyDown(Keys.P))
                {
                    Console.WriteLine("Open!");
                }

                if (keyboardState.IsKeyDown(Keys.A) || (currentState.DPad.Left == ButtonState.Pressed))
                {
                    // Rotate left.
                    avatarYaw += rotationSpeed;
                }

                if (keyboardState.IsKeyDown(Keys.D) || (currentState.DPad.Right == ButtonState.Pressed))
                {
                    // Rotate right.
                    avatarYaw -= rotationSpeed;
                }

                if (keyboardState.IsKeyDown(Keys.W) || (currentState.DPad.Up == ButtonState.Pressed))
                {
                    Matrix forwardMovement = Matrix.CreateRotationY(avatarYaw);
                    Vector3 v = new Vector3(0, 0, forwardSpeed);
                    v = Vector3.Transform(v, forwardMovement);
                    avatarPosition.Z += v.Z;
                    avatarPosition.X += v.X;
                }

                if (keyboardState.IsKeyDown(Keys.S) || (currentState.DPad.Down == ButtonState.Pressed))
                {
                    Matrix forwardMovement = Matrix.CreateRotationY(avatarYaw);
                    Vector3 v = new Vector3(0, 0, -forwardSpeed);
                    v = Vector3.Transform(v, forwardMovement);
                    avatarPosition.Z += v.Z;
                    avatarPosition.X += v.X;
                }

                if (keyboardState.IsKeyDown(Keys.F))
                {
                    avatarPosition.Y = avatarPosition.Y - 1;
                }

                if (keyboardState.IsKeyDown(Keys.R) || keyboardState.IsKeyDown(Keys.Space))
                {
                    avatarPosition.Y = avatarPosition.Y + 1;
                }

                if (keyboardState.IsKeyDown(Keys.E))
                {
                    Matrix forwardMovement = Matrix.CreateRotationY(avatarYaw);
                    Vector3 v = new Vector3(1, 0, 0);
                    v = Vector3.Transform(v, forwardMovement);
                    avatarPosition.X -= v.X;
                    avatarPosition.Y -= v.Y;
                    avatarPosition.Z -= v.Z;
                }

                if (keyboardState.IsKeyDown(Keys.Q))
                {
                    Matrix forwardMovement = Matrix.CreateRotationY(avatarYaw);
                    Vector3 v = new Vector3(1, 0, 0);
                    v = Vector3.Transform(v, forwardMovement);
                    avatarPosition.X += v.X;
                    avatarPosition.Y += v.Y;
                    avatarPosition.Z += v.Z;
                }

                if (keyboardState.IsKeyDown(Keys.T))
                {
                    thirdPersonReference.Y = thirdPersonReference.Y - 0.25f;
                }
                if (keyboardState.IsKeyDown(Keys.G))
                {
                    thirdPersonReference.Y = thirdPersonReference.Y + 0.25f;
                }
            }
        }

        void UpdateCameraThirdPerson()
        {
            Matrix rotationMatrix = Matrix.CreateRotationY(avatarYaw);

            // Create a vector pointing the direction the camera is facing.
            Vector3 transformedReference = Vector3.Transform(thirdPersonReference, rotationMatrix);

            // Calculate the position the camera is looking from.
            Vector3 cameraPosition = transformedReference + avatarPosition;

            // Set up the view matrix and projection matrix.
            view = Matrix.CreateLookAt(cameraPosition, avatarPosition, new Vector3(0.0f, 1.0f, 0.0f));

            Viewport viewport = graphics.GraphicsDevice.Viewport;
            float aspectRatio = (float)viewport.Width / (float)viewport.Height;

            proj = Matrix.CreatePerspectiveFieldOfView(viewAngle, aspectRatio, nearClip, farClip);
        }
   

    }
}