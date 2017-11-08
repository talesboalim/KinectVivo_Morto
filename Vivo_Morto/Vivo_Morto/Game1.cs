using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using Microsoft.Kinect;

namespace Vivo_Morto
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class Game1 : Microsoft.Xna.Framework.Game
    {
        const int screenWidth = 1024;
        const int screenHeight = 768;

        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;

        KinectSensor kinect;
        bool KinectDetectado = false;

        //Funciona o vídeo
        Texture2D colorVideo;

        //Funciona o esqueleto
        Skeleton[] skeletons;
        Skeleton trackedSkeleton1;
        Skeleton trackedSkeleton2;
        bool _jogador1Detectado = false, _jogador2Detectado = false;

        Texture2D texJoint1;
        Texture2D texJoint2;

        Boolean ligarCamera = false;

        //pressionou tecla
        KeyboardState oldState;

        //jogo
        int Cena = 1;
        TimeSpan _tempoMensagem;
        int _jogador1Ponto, _jogador2Ponto;
        int _jogador1Posicao, _jogador2Posicao;
        float _jogador1LimiteAbaixado, _jogador2LimiteAbaixado;
        int _posicao, _posicaoAnterior;
        int _posicaoExibir;
        int _qtdeSorteou = 0;
        int _vencedor = 0;
        Boolean _pontoNegativoGanhou;

        //fontes
        SpriteFont sFontMedia, sFontGrande, sFontGigante, sFontInstrucao;


        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            graphics.IsFullScreen = false;
            graphics.PreferredBackBufferWidth = screenWidth;
            graphics.PreferredBackBufferHeight = screenHeight;
            Content.RootDirectory = "Content";
        }

        protected override void Initialize()
        {
            TransformSmoothParameters parameters = new TransformSmoothParameters();
            parameters.Smoothing = 0.7f;
            parameters.Correction = 0.3f;
            parameters.Prediction = 0.4f;
            parameters.JitterRadius = 1.0f;
            parameters.MaxDeviationRadius = 0.5f;



            //Inicializa o sensor
            try
            {
                kinect = KinectSensor.KinectSensors[0];
                if (ligarCamera == true)
                {
                    //Inicializa o vídeo
                    kinect.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);
                    //kinect.ColorStream.Enable(ColorImageFormat.YuvResolution640x480Fps15);
                    kinect.ColorFrameReady += new EventHandler<ColorImageFrameReadyEventArgs>(kinect_ColorFrameReady);
                }
                //Inicializa o esqueleto
                kinect.SkeletonStream.Enable();
                //Inicia tudo
                kinect.Start();

                kinect.SkeletonFrameReady += new EventHandler<SkeletonFrameReadyEventArgs>(kinect_SkeletonFrameReady);
                KinectDetectado = true;
            }
            catch {
                KinectDetectado = true;
            }
            
            base.Initialize();
        }

        protected override void OnExiting(Object sender, EventArgs args)
        {
            base.OnExiting(sender, args);

            kinect.Stop();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);

            // TODO: use this.Content to load your game content here
            texJoint1 = Content.Load<Texture2D>("joint1");
            texJoint2 = Content.Load<Texture2D>("joint2");

            sFontMedia = Content.Load<SpriteFont>("SpriteFontMedia");
            sFontGrande = Content.Load<SpriteFont>("SpriteFontGrande");
            sFontGigante = Content.Load<SpriteFont>("SpriteFontGigante");
            sFontInstrucao = Content.Load<SpriteFont>("SpriteFontInstrucao");
        }

        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }

        protected override void Update(GameTime gameTime)
        {
            // Allows the game to exit
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
                this.Exit();

            // TODO: Add your update logic here

            //se o usuário apertar cima ou baixo muda o angulo do kinect
            sensorConfigurarAngulo();

            _tempoMensagem += gameTime.ElapsedGameTime;

            if (Cena == 1)//tela inicial
            {
                //verifica se os 2 jogadores estão com a mão na cabeça, se estiverem inicia o jogo
                if (kinPosicaoMaoAcimaCabeca(1) == true && kinPosicaoMaoAcimaCabeca(2) == true)
                {
                    //usado na função jogoAguardar
                    _tempoMensagem = TimeSpan.Zero;
                    jogoCenaPassarProxima(10);
                }

            }
            else if (Cena == 10)//aguardando
            {
                //aguarda 3 seguntos antes de iniciar o jogo
                if (jogoAguardar(5) == false)
                {
                    jogoCenaPassarProxima(20);
                    jogoIniciar();
                }

            }
            else if (Cena == 20)//jogando
            {
                if (jogoAcabou() == false)
                {
                    //verifica a posição do jogadorz
                    kinJogadorPosicao();

                    //se não é para exibir na tela, então aguarda 3 segundos antes de exibir
                    if (_posicaoExibir == 0)
                    {
                        if (jogoAguardar(2) == false)
                        {
                            _posicaoExibir = 1;

                            //salva a posição anterior para saber se irá contar ponto
                            _posicaoAnterior = _posicao;

                            jogoSortearPosicao();

                            _pontoNegativoGanhou = false;
                        }
                    }
                    else if (_posicaoExibir == 1)
                    {
                        //se é a mesma opção então os 2 acertam e ninguém ganha ponto
                        if (_posicaoAnterior == _posicao)
                        {
                            _posicaoExibir = 3;
                            _tempoMensagem = TimeSpan.Zero;
                        }
                        else
                        {
                            if (jogoContarPonto() == true)
                            {
                                _posicaoExibir = 2;
                            }
                        }
                    }
                    else if (_posicaoExibir == 2)
                    {
                        if (jogoJogadoresNaMesmaPosicao() == true)
                        {
                            _posicaoExibir = 3;
                            _tempoMensagem = TimeSpan.Zero;
                        }
                    }
                    else
                    {
                        jogoContarPontoNegativo();
                        if (jogoJogadoresNaMesmaPosicao() == true)
                        {
                            if (jogoAguardar(2) == false)
                            {
                                _posicaoExibir = 0;
                                //zera para nao aparecer nada na tela
                                _tempoMensagem = TimeSpan.Zero;
                            }
                        }
                    }
                }
                else
                {
                    jogoCenaPassarProxima(50);
                    _tempoMensagem = TimeSpan.Zero;
                }


            }
            else if (Cena == 50)//jogando
            {
                if (jogoAguardar(5) == false)
                {
                    jogoIniciar();
                    Cena = 1;
                }
            }

            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);

            spriteBatch.Begin();

            //Ligar câmera
            if (ligarCamera == true)
            {
                spriteBatch.Draw(colorVideo, new Rectangle(0, 0, screenWidth, screenHeight), Color.White);
            }
            /*
            // Draw Jogador 1
            if (trackedSkeleton1 != null)
            {
                foreach (Joint joint in trackedSkeleton1.Joints)
                {
                    Joint scaledJoint = joint.Scale(screenWidth, screenHeight);
                    Vector2 position = new Vector2(scaledJoint.Position.X, scaledJoint.Position.Y);
                    spriteBatch.Draw(texJoint1, position, null, Color.Red, 0f, Vector2.Zero, 0.5f / scaledJoint.Position.Z, SpriteEffects.None, 0);
                }
            }
            
            // Draw Jogador 2
            if (trackedSkeleton2 != null)
            {
                foreach (Joint joint in trackedSkeleton2.Joints)
                {
                    Joint scaledJoint = joint.Scale(screenWidth, screenHeight);
                    Vector2 position = new Vector2(scaledJoint.Position.X, scaledJoint.Position.Y);
                    spriteBatch.Draw(texJoint2, position, null, Color.Blue, 0f, Vector2.Zero, 0.5f / scaledJoint.Position.Z, SpriteEffects.None, 0);
                }
            }
            */
            //JOGO
            if (Cena == 1)
            {
                //nome do jogo
                spriteBatch.DrawString(sFontGrande, "VIVO", new Vector2(100, 10), Color.Blue);
                spriteBatch.DrawString(sFontGrande, "ou", new Vector2(425, 10), Color.White);
                spriteBatch.DrawString(sFontGrande, "MORTO", new Vector2(600, 10), Color.Red);
                //kinect
                if (KinectDetectado == false)
                {
                    spriteBatch.DrawString(sFontMedia, "Kinect não detectado!", new Vector2(200, 600), Color.White);
                }

                spriteBatch.DrawString(sFontMedia, "Jogador 1", new Vector2(50, 130), Color.White);
                spriteBatch.DrawString(sFontMedia, "Jogador 2", new Vector2(520, 130), Color.White);
                //detecta jogador 1
                if (_jogador1Detectado == false)
                {
                    spriteBatch.DrawString(sFontMedia, "MORTO", new Vector2(100, 180), Color.Red);
                }
                else
                {
                    spriteBatch.DrawString(sFontMedia, "VIVO", new Vector2(100, 180), Color.Blue);

                    if (kinPosicaoMaoAcimaCabeca(1) == false)
                    {
                        spriteBatch.DrawString(sFontMedia, "Levante a mão", new Vector2(50, 260), Color.Lime);
                        spriteBatch.DrawString(sFontMedia, "  acima da", new Vector2(50, 300), Color.Lime);
                        spriteBatch.DrawString(sFontMedia, "   cabeça", new Vector2(50, 340), Color.Lime);
                        spriteBatch.DrawString(sFontMedia, " para começar", new Vector2(50, 380), Color.Lime);
                    }
                    else
                    {
                        spriteBatch.DrawString(sFontMedia, "Aguarde", new Vector2(50, 260), Color.White);
                    }
                }
                //detecta jogador 2
                if (_jogador2Detectado == false)
                {
                    spriteBatch.DrawString(sFontMedia, "MORTO", new Vector2(520, 180), Color.Red);
                }
                else
                {
                    spriteBatch.DrawString(sFontMedia, "VIVO", new Vector2(520, 180), Color.Blue);

                    if (kinPosicaoMaoAcimaCabeca(2) == false)
                    {
                        spriteBatch.DrawString(sFontMedia, "Levante a mão", new Vector2(510, 260), Color.Lime);
                        spriteBatch.DrawString(sFontMedia, "  acima da", new Vector2(510, 300), Color.Lime);
                        spriteBatch.DrawString(sFontMedia, "   cabeça", new Vector2(510, 340), Color.Lime);
                        spriteBatch.DrawString(sFontMedia, " para começar", new Vector2(510, 380), Color.Lime);
                    }
                    else
                    {
                        spriteBatch.DrawString(sFontMedia, "Aguarde", new Vector2(510, 260), Color.White);
                    }
                }

                spriteBatch.DrawString(sFontInstrucao, "   O computador dará 2 comandos: VIVO ou MORTO.", new Vector2(10, 500), Color.White);
                spriteBatch.DrawString(sFontInstrucao, "VIVO", new Vector2(100, 530), Color.Blue);
                spriteBatch.DrawString(sFontInstrucao, " = todos tem de ficar em pé", new Vector2(250, 530), Color.White);
                spriteBatch.DrawString(sFontInstrucao, "MORTO", new Vector2(100, 560), Color.Red);
                spriteBatch.DrawString(sFontInstrucao, " = todos têm de agachar ", new Vector2(250, 560), Color.White);
                spriteBatch.DrawString(sFontInstrucao, "   Quem fizer o comando primeiro ganha 1 ponto, quem errar", new Vector2(10, 590), Color.White);
                spriteBatch.DrawString(sFontInstrucao, "perde 1 ponto. ", new Vector2(10, 620), Color.White);
                spriteBatch.DrawString(sFontInstrucao, "   Será o vencedor quem fizer 5 pontos primeiro.", new Vector2(10, 650), Color.White);

            }
            else if (Cena == 10)
            {
                spriteBatch.DrawString(sFontGrande, "PREPARE-SE", new Vector2(120, 150), Color.Red);
                spriteBatch.DrawString(sFontMedia, "Ganha quem fizer 5 pontos!", new Vector2(120, 500), Color.White);
            }
            else if (Cena == 20)
            {
                //spriteBatch.DrawString(sFontMedia, "Pontos", new Vector2(50, 550), Color.Red);
                spriteBatch.DrawString(sFontMedia, "Jogador 1:", new Vector2(50, 630), Color.White);
                spriteBatch.DrawString(sFontGrande, _jogador1Ponto.ToString(), new Vector2(380, 580), Color.White);
                spriteBatch.DrawString(sFontMedia, "Jogador 2:", new Vector2(520, 630), Color.White);
                spriteBatch.DrawString(sFontGrande, _jogador2Ponto.ToString(), new Vector2(850, 580), Color.White);

                //mensagem  de vivo ou morto
                if (_posicaoExibir >= 1)
                {
                    if (_posicao == 1)
                    {
                        spriteBatch.DrawString(sFontGigante, "VIVO", new Vector2(280, 150), Color.Blue);
                    }
                    else if (_posicao == 2)
                    {
                        spriteBatch.DrawString(sFontGigante, "MORTO", new Vector2(280, 150), Color.Red);
                    }
                }

                //posição do jogador
                if (_jogador1Posicao == 1)
                {
                    spriteBatch.DrawString(sFontMedia, "Vivo", new Vector2(50, 580), Color.Blue);
                }
                else
                {
                    spriteBatch.DrawString(sFontMedia, "Morto", new Vector2(50, 580), Color.Red);
                }
                if (_jogador2Posicao == 1)
                {
                    spriteBatch.DrawString(sFontMedia, "Vivo", new Vector2(520, 580), Color.Blue);
                }
                else
                {
                    spriteBatch.DrawString(sFontMedia, "Morto", new Vector2(520, 580), Color.Red);
                }

            }
            else if (Cena == 50)
            {

                spriteBatch.DrawString(sFontGigante, "Acabou!", new Vector2(170, 50), Color.Lime);
                spriteBatch.DrawString(sFontGigante, "Jogador " + _vencedor, new Vector2(70, 300), Color.White);
                spriteBatch.DrawString(sFontGigante, "ganhou", new Vector2(210, 450), Color.White);

            }
            /*
            falta exibir quem ganhou
            colocar as funcoes do kinect
            se tem`uma diferenca de 5 pontos entao acaba o jogo
            */

            


            spriteBatch.End();



            base.Draw(gameTime);
        }

        void kinect_ColorFrameReady(object sender, ColorImageFrameReadyEventArgs colorImageFrame)
        {
            //Get raw image
            ColorImageFrame colorVideoFrame = colorImageFrame.OpenColorImageFrame();

            if (colorVideoFrame != null)
            {
                //Create array for pixel data and copy it from the image frame
                Byte[] pixelData = new Byte[colorVideoFrame.PixelDataLength];
                colorVideoFrame.CopyPixelDataTo(pixelData);

                //Convert RGBA to BGRA
                Byte[] bgraPixelData = new Byte[colorVideoFrame.PixelDataLength];
                for (int i = 0; i < pixelData.Length; i += 4)
                {
                    bgraPixelData[i] = pixelData[i + 2];
                    bgraPixelData[i + 1] = pixelData[i + 1];
                    bgraPixelData[i + 2] = pixelData[i];
                    bgraPixelData[i + 3] = (Byte)255; //The video comes with 0 alpha so it is transparent
                }

                // Create a texture and assign the realigned pixels
                colorVideo = new Texture2D(graphics.GraphicsDevice, colorVideoFrame.Width, colorVideoFrame.Height);
                colorVideo.SetData(bgraPixelData);
            }
        }

        void kinect_SkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            using (SkeletonFrame skeletonFrame = e.OpenSkeletonFrame())
            {
                if (skeletonFrame != null)
                {
                    if (skeletons == null)
                    {
                        skeletons = new Skeleton[skeletonFrame.SkeletonArrayLength];
                    }

                    skeletonFrame.CopySkeletonDataTo(skeletons);

                    trackedSkeleton1 = skeletons.Where(s => s.TrackingState == SkeletonTrackingState.Tracked).FirstOrDefault();
                    if (trackedSkeleton1 != null)
                    {
                        _jogador1Detectado = true;


                        trackedSkeleton2 = skeletons.Where(s => s.TrackingState == SkeletonTrackingState.Tracked).LastOrDefault();
                        if (trackedSkeleton1.TrackingId != trackedSkeleton2.TrackingId)
                        {
                            if (trackedSkeleton2 != null)
                            {
                                _jogador2Detectado = true;
                            }
                            else
                            {
                                _jogador2Detectado = false;
                            }
                        }
                    }
                    else
                    {
                        _jogador1Detectado = false;
                        _jogador2Detectado = false;
                    }
                }
            }
        }

        void sensorConfigurarAngulo()
        {
            KeyboardState ks = Keyboard.GetState();

            if (ks.IsKeyDown(Keys.Up))
            {
                //então verifica se o usuário soltou a tecla
                if (!oldState.IsKeyDown(Keys.Up))
                {
                    if (KinectDetectado == true)
                    {
                        try
                        {
                            kinect.ElevationAngle = kinect.ElevationAngle + 3;
                        }
                        catch { }
                    }
                }
            }
            else if (ks.IsKeyDown(Keys.Down))
            {
                //então verifica se o usuário soltou a tecla
                if (!oldState.IsKeyDown(Keys.Down))
                {
                    if (KinectDetectado == true)
                    {
                        try
                        {
                            kinect.ElevationAngle = kinect.ElevationAngle - 3;
                        }
                        catch { }
                    }
                }

            }
        }

        Boolean kinPosicaoMaoAcimaCabeca(int jogador)
        {
            Boolean retorno = false;

            KeyboardState ks = Keyboard.GetState();

            if (jogador == 1)
            {
                if (trackedSkeleton1 != null)
                {

                    if (trackedSkeleton1.Joints[JointType.HandRight].Position.Y > trackedSkeleton1.Joints[JointType.Head].Position.Y)
                    {
                        //verifica o limite que o jogador abaixa e fica MORTO
                        _jogador1LimiteAbaixado = trackedSkeleton1.Joints[JointType.ShoulderRight].Position.Y - ((Math.Abs(trackedSkeleton1.Joints[JointType.ShoulderRight].Position.Y) + Math.Abs(trackedSkeleton1.Joints[JointType.AnkleRight].Position.Y)) / 3);
                        retorno = true;
                    }
                }
                if (ks.IsKeyDown(Keys.A))
                {
                    retorno = true;
                }
            }
            else if (jogador == 2)
            {
                if (trackedSkeleton2 != null)
                {

                    if (trackedSkeleton2.Joints[JointType.HandRight].Position.Y > trackedSkeleton2.Joints[JointType.Head].Position.Y)
                    {
                        //verifica o limite que o jogador abaixa e fica MORTO
                        _jogador2LimiteAbaixado = trackedSkeleton2.Joints[JointType.ShoulderRight].Position.Y - ((Math.Abs(trackedSkeleton2.Joints[JointType.ShoulderRight].Position.Y) + Math.Abs(trackedSkeleton2.Joints[JointType.AnkleRight].Position.Y)) / 3);
                        retorno = true;
                    }
                }

                if (ks.IsKeyDown(Keys.K))
                {
                    retorno = true;
                }
            }

            return (retorno);
        }


        Boolean jogoAguardar(int tempo)
        {
            if (_tempoMensagem <= TimeSpan.FromSeconds(tempo))
            {
                return (true);
            }
            else
            {
                return (false);
            }
        }

        void jogoCenaPassarProxima(int _Cena)
        {
            Cena = _Cena;
        }

        void jogoIniciar()
        {            
            _jogador1Ponto = 0;
            _jogador2Ponto = 0;
            _jogador1Posicao = 1;
            _jogador2Posicao = 1;
            _posicao = 0;
            _posicaoExibir = 0;
            _qtdeSorteou = 0;
            _vencedor = 0;
            _tempoMensagem = TimeSpan.Zero;
            _pontoNegativoGanhou = false;
        }

        Boolean jogoAcabou()
        {
            if (_jogador1Ponto == 5)
            {
                _vencedor = 1;
                return (true);
            }
            else if (_jogador2Ponto == 5)
            {
                _vencedor = 2;
                return (true);
            }
            else
            {
                return (false);
            }
        }

        Boolean jogoContarPonto()
        {
            if (_jogador1Posicao == _posicao && _jogador2Posicao == _posicao)
            {
                return (true);
            }
            else if (_jogador1Posicao == _posicao)
            {
                _jogador1Ponto++;
                return (true);
            }
            else if (_jogador2Posicao == _posicao)
            {
                _jogador2Ponto++;
                return (true);
            }
            else
            {
                return (false);
            }
        }

        Boolean jogoJogadoresNaMesmaPosicao()
        {
            // os 2 jogadores devem estar na mesma posição e também na posição indicada pelo jogador
            if ((_jogador1Posicao == _posicao) && (_jogador2Posicao == _posicao))
            {
                return (true);
            }
            else
            {
                return (false);
            }
        }

        Boolean jogoContarPontoNegativo()
        {
            if (_pontoNegativoGanhou == false)
            {
                if ((_jogador1Posicao == _posicao) && (_jogador2Posicao != _posicao))
                {
                    _jogador2Ponto--;
                    _pontoNegativoGanhou = true;
                    return (true);
                }
                else if ((_jogador1Posicao != _posicao) && (_jogador2Posicao == _posicao))
                {
                    _jogador1Ponto--;
                    _pontoNegativoGanhou = true;
                    return (true);
                }
                else
                {
                    return (false);
                }
            }
            else
            {
                return (false);
            }
        }

        void kinJogadorPosicao()
        {
            KeyboardState ks = Keyboard.GetState();

            if (trackedSkeleton1 != null)
            {
                if (trackedSkeleton1.Joints[JointType.ShoulderRight].Position.Y > _jogador1LimiteAbaixado)
                {
                    _jogador1Posicao = 1;
                }
                else
                {
                    _jogador1Posicao = 2;
                }

            }
            else
            {
                if (ks.IsKeyDown(Keys.A))
                {
                    //então verifica se o usuário soltou a tecla
                    if (!oldState.IsKeyDown(Keys.A))
                    {
                        _jogador1Posicao = 1;
                    }
                }
                else if (ks.IsKeyDown(Keys.Z))
                {
                    //então verifica se o usuário soltou a tecla
                    if (!oldState.IsKeyDown(Keys.Z))
                    {
                        _jogador1Posicao = 2;
                    }
                }
            }
            if (trackedSkeleton2 != null)
            {
                if (trackedSkeleton2.Joints[JointType.ShoulderRight].Position.Y > _jogador2LimiteAbaixado)
                {
                    _jogador2Posicao = 1;
                }
                else
                {
                    _jogador2Posicao = 2;
                }

            }
            else
            {
                //teclado usado para teste
                if (ks.IsKeyDown(Keys.K))
                {
                    //então verifica se o usuário soltou a tecla
                    if (!oldState.IsKeyDown(Keys.K))
                    {
                        _jogador2Posicao = 1;
                    }
                }
                else if (ks.IsKeyDown(Keys.M))
                {
                    //então verifica se o usuário soltou a tecla
                    if (!oldState.IsKeyDown(Keys.M))
                    {
                        _jogador2Posicao = 2;
                    }
                }
            }
        }

        void jogoSortearPosicao()
        {
            //sorteia a posição: 1(vivo) ou 2(morto)                            
            Random rnd = new Random(DateTime.Now.Millisecond);
            _posicao = rnd.Next(1, 3);

            _qtdeSorteou++;
            //não deixa o computador sortear mais que 3 vezes o mesmo número
            if (_qtdeSorteou == 3)
            {
                if (_posicao == 1)
                {
                    _posicao = 2;
                }
                else
                {
                    _posicao = 1;
                }
            }
        }


    }

    internal static class ExtensionMethods
    {
        public static Joint Scale(this Joint joint, int width, int height)
        {
            SkeletonPoint skeletonPoint = new SkeletonPoint()
            {
                X = Scale(joint.Position.X, width),
                Y = Scale(-joint.Position.Y, height),
                Z = joint.Position.Z
            };

            Joint scaledJoint = new Joint()
            {
                TrackingState = joint.TrackingState,
                Position = skeletonPoint
            };

            return scaledJoint;
        }

        public static float Scale(float value, int max)
        {
            return (max >> 1) + (value * (max >> 1));
        }
    }
}
