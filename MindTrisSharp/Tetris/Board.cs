﻿using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Diagnostics;

namespace Tetris
{
    class Board : Microsoft.Xna.Framework.DrawableGameComponent
    {
        SpriteBatch sBatch;
        Texture2D textures;
        Rectangle[] _rectangles;

        FieldState[,] _boardFields;
        Vector2[, ,] _figures;
        Vector2[,] _jlstz_offsetData;
        Vector2[,] _i_offsetData;
        Vector2[,] _o_offsetData;

        readonly Vector2 StartPositionForNewFigureLeft = new Vector2(3, 0);
        Vector2 PositionForDynamicFigure;
        Vector2[] DynamicFigure = new Vector2[BLOCKS_COUNT_IN_FIGURE];
        Random random = new Random();
        int[,] _boardColor;
        public const int HEIGHT = 20;
        public const int WIDTH = 10;
        const int BLOCKS_COUNT_IN_FIGURE = 4;
        int DynamicFigureNumber;
        int DynamicFigureModificationNumber;
        int DynamicFigureColor;
        bool BlockLine;
        bool showNewBlock;
        float movement;
        float speed;

        //Gestion des pieces
        Queue<byte> _nextFigures = new Queue<byte>();
        uint _piecesPlacedCount = 0;

        public delegate void MoveExecutedFunction(uint pieceNumber, byte orientation, byte x, byte y);
        public event MoveExecutedFunction MoveExecuted;
        public delegate void VoidFunction();
        public event VoidFunction PiecesRequired;
        public delegate void IntFunction(int n);
        public event IntFunction LinesDeleted;

        public float Movement
        {
            set { movement = value; }
            get { return movement; }
        }
        public float Speed
        {
            set { speed = value; }
            get { return speed; }
        }

        public Board(Game game, byte[] pieces, ref Texture2D textures, Rectangle[] rectangles) 
            : base(game)
        {
            sBatch = (SpriteBatch)Game.Services.GetService(typeof(SpriteBatch));
            // Load textures for blocks
            this.textures = textures;

            // Rectangles to draw figures
            _rectangles = rectangles;

            //_figures = Tetrominos.CreateFigures();
            _figures = Tetrominos.CreateFiguresSRSTrueRotations();
            Tetrominos.CreateSRSTrueRotationsOffsetData(out _jlstz_offsetData, out _i_offsetData, out _o_offsetData);

            // Create tetris board
            _boardFields = new FieldState[WIDTH, HEIGHT];
            _boardColor = new int[WIDTH, HEIGHT];

            for (int i = 0; i < pieces.Length; i++)
            {
                _nextFigures.Enqueue(pieces[i]);
            }
        }

        public override void Initialize()
        {
            showNewBlock = true;
            movement = 0;

            for (int i = 0; i < WIDTH; i++)
                for (int j = 0; j < HEIGHT; j++)
                    ClearBoardField(i, j);
            
            base.Initialize();
        }

        public void FeedPieces(byte[] pieces)
        {
            lock (_nextFigures)
            {
                for (int i = 0; i < pieces.Length; i++)
                {
                    _nextFigures.Enqueue(pieces[i]);
                }
            }
        }

        public void FindDynamicFigure()
        {
            int BlockNumberInDynamicFigure = 0;
            for (int i = 0; i < WIDTH; i++)
                for (int j = 0; j < HEIGHT; j++)
                    if (_boardFields[i, j] == FieldState.Dynamic)
                        DynamicFigure[BlockNumberInDynamicFigure++] = new Vector2(i, j);
        }

        /// <summary>
        /// Find, destroy and save lines's count
        /// </summary>
        /// <returns>Number of destoyed lines</returns>
        public int DestroyLines()
        {
            // Find total lines
            int BlockLineCount = 0;
            for (int j = 0; j < HEIGHT; j++)
            {
                for (int i = 0; i < WIDTH; i++)
                    if (_boardFields[i, j] == FieldState.Static)
                        BlockLine = true;
                    else
                    {
                        BlockLine = false;
                        break;
                    }
                //Destroy total lines
                if (BlockLine)
                {
                    // Save number of total lines
                    BlockLineCount++;
                    for (int l = j; l > 0; l--)
                        for (int k = 0; k < WIDTH; k++)
                        {
                            CopyBlock(k, l, k, l - 1);
                        }
                    for (int l = 0; l < WIDTH; l++)
                    {
                        _boardFields[l, 0] = FieldState.Free;
                        _boardColor[l, 0] = -1;
                    }
                }
            }
            if (BlockLineCount > 0)
            {
                if (LinesDeleted != null) LinesDeleted(BlockLineCount);
            }
            return BlockLineCount;
        }

        /// <summary>
        /// Create new shape in the game, if need it
        /// </summary>
        public bool CreateNewFigure()
        {
            if (showNewBlock)
            {
                //On redemande d'autres pièces s'il n'en reste plus beaucoup
                if (_nextFigures.Count < 10) if (PiecesRequired != null) PiecesRequired();

                if (_nextFigures.Count == 0) return false;
                // Generate new figure's shape
                DynamicFigureNumber = _nextFigures.Dequeue();

                DynamicFigureModificationNumber = 0;
                //nextFiguresModification.Dequeue();
                //extFiguresModification.Enqueue(random.Next(4));

                DynamicFigureColor = DynamicFigureNumber;

                // Position and coordinates for new dynamic figure
                PositionForDynamicFigure = StartPositionForNewFigureLeft + new Vector2((DynamicFigureNumber == 3 ? 1 : 0), 0);
                for (int i = 0; i < BLOCKS_COUNT_IN_FIGURE; i++)
                    DynamicFigure[i] = _figures[DynamicFigureNumber, DynamicFigureModificationNumber, i] + PositionForDynamicFigure;

                if (!DrawFigureOnBoard(DynamicFigure, DynamicFigureColor))
                    return false;

                showNewBlock = false;
            }
            return true;
        }
        bool DrawFigureOnBoard(Vector2[] vector, int color)
        {
            if (!TryPlaceFigureOnBoard(vector))
                return false;
            for (int i = 0; i <= vector.GetUpperBound(0); i++)
            {
                _boardFields[(int)vector[i].X, (int)vector[i].Y] = FieldState.Dynamic;
                _boardColor[(int)vector[i].X, (int)vector[i].Y] = color;
            }
            return true;
        }

        bool TryPlaceFigureOnBoard(Vector2[] vector)
        {
            for (int i = 0; i <= vector.GetUpperBound(0); i++)
                if ((vector[i].X < 0) || (vector[i].X >= WIDTH) ||
                    (vector[i].Y >= HEIGHT))
                    return false;
            for (int i = 0; i <= vector.GetUpperBound(0); i++)
                if (_boardFields[(int)vector[i].X, (int)vector[i].Y] == FieldState.Static)
                    return false;
            return true;
        }
        
        public void MoveFigureLeft()
        {
            // Sorting blocks fro dynamic figure to correct moving
            SortingVector2(ref DynamicFigure, true, DynamicFigure.GetLowerBound(0), 
                DynamicFigure.GetUpperBound(0));
            // Check colisions
            for (int i = 0; i < BLOCKS_COUNT_IN_FIGURE; i++)
            {
                if ((DynamicFigure[i].X == 0))
                    return;
                if (_boardFields[(int)DynamicFigure[i].X - 1, (int)DynamicFigure[i].Y] == FieldState.Static)
                    return;
            }
            // Move figure on board
            for (int i = 0; i < BLOCKS_COUNT_IN_FIGURE; i++)
            {
                _boardFields[(int)DynamicFigure[i].X - 1, (int)DynamicFigure[i].Y] =
                    _boardFields[(int)DynamicFigure[i].X, (int)DynamicFigure[i].Y];
                _boardColor[(int)DynamicFigure[i].X - 1, (int)DynamicFigure[i].Y] = 
                    _boardColor[(int)DynamicFigure[i].X, (int)DynamicFigure[i].Y];
                ClearBoardField((int)DynamicFigure[i].X, (int)DynamicFigure[i].Y);
                // Change position for blocks in DynamicFigure
                DynamicFigure[i].X = DynamicFigure[i].X - 1;
            }
            // Change position vector
            //if (PositionForDynamicFigure.X > 0)
            PositionForDynamicFigure.X--;
        }

        public void MoveFigureRight()
        {
            // Sorting blocks fro dynamic figure to correct moving
            SortingVector2(ref DynamicFigure, true, DynamicFigure.GetLowerBound(0), 
                DynamicFigure.GetUpperBound(0));
            // Check colisions
            for (int i = 0; i < BLOCKS_COUNT_IN_FIGURE; i++)
            {
                if ((DynamicFigure[i].X == WIDTH - 1))
                    return;
                if (_boardFields[(int)DynamicFigure[i].X + 1, (int)DynamicFigure[i].Y] == FieldState.Static)
                    return;
            }
            // Move figure on board
            for (int i = BLOCKS_COUNT_IN_FIGURE - 1; i >=0; i--)
            {
                _boardFields[(int)DynamicFigure[i].X + 1, (int)DynamicFigure[i].Y] =
                    _boardFields[(int)DynamicFigure[i].X, (int)DynamicFigure[i].Y];
                _boardColor[(int)DynamicFigure[i].X + 1, (int)DynamicFigure[i].Y] =
                    _boardColor[(int)DynamicFigure[i].X, (int)DynamicFigure[i].Y];
                ClearBoardField((int)DynamicFigure[i].X, (int)DynamicFigure[i].Y);
                // Change position for blocks in DynamicFigure
                DynamicFigure[i].X = DynamicFigure[i].X + 1;
            }
            // Change position vector
            //if (PositionForDynamicFigure.X < width - 1)
            PositionForDynamicFigure.X++;
        }

        public void HardDrop()
        {
            Vector2[] oldDynamic = new Vector2[BLOCKS_COUNT_IN_FIGURE];
            DynamicFigure.CopyTo(oldDynamic, 0);

            DynamicFigure = GetGhostPiece(DynamicFigure);

            FixPieceAndSendEvent();
            showNewBlock = true;
            //Grab color
            int color = _boardColor[(int)oldDynamic[0].X, (int)oldDynamic[0].Y];
            //Delete former piece
            for (int i = 0; i < BLOCKS_COUNT_IN_FIGURE; i++)
            {
                ClearBoardField((int)oldDynamic[i].X, (int)oldDynamic[i].Y);
            }
            // Move figure on board
            for (int i = 0; i < BLOCKS_COUNT_IN_FIGURE; i++)
            {
                _boardFields[(int)DynamicFigure[i].X, (int)DynamicFigure[i].Y] = FieldState.Static;
                _boardColor[(int)DynamicFigure[i].X, (int)DynamicFigure[i].Y] = color;
            }

            PositionForDynamicFigure.Y += (DynamicFigure[0].Y - oldDynamic[0].Y);
            return;
        }

        public void ApplyPenalty(Penalty penalty)
        {
            int count = penalty.Lines - 1;
            int y;
            //On recopie les lignes
            for (y = 0; y < HEIGHT - count; y++)
            {
                for (int x = 0; x < WIDTH; x++)
                {
                    CopyBlock(x, y, x, y + count);
                }
            }
            //On ajoute des lignes grises avec un trou
            for (; y < HEIGHT; y++)
            {
                int holeX = penalty.NextHoleX();
                for (int x = 0; x < WIDTH; x++)
                {
                    if (x != holeX)
                    {
                        _boardFields[x, y] = FieldState.Static;
                        _boardColor[x, y] = _rectangles.Length - 1;
                    }
                    else
                    {
                        _boardFields[x, y] = FieldState.Free;
                        _boardColor[x, y] = -1;
                    }
                }
            }
        }

        Vector2 GetPositionOfPlacedPiece(Vector2[] piece)
        {
            float Y_max = float.MinValue;
            for (int i = 0; i < piece.Length; i++)
            {
                if (piece[i].Y > Y_max) Y_max = piece[i].Y;
            }
            //On a chopé le truc le plus bas, on chope le truc le plus à gauche
            float X_min = float.MaxValue;
            for (int i = 0; i < piece.Length; i++)
            {
                if (piece[i].X < X_min) X_min = piece[i].X;
            }
            return new Vector2(X_min, Y_max);
        }

        Vector2 GetPositionOfSRSPiece(Vector2[] piece)
        {
            float Y_min = float.MaxValue;
            for (int i = 0; i < piece.Length; i++)
            {
                if (piece[i].Y < Y_min) Y_min = piece[i].Y;
            }
            //On a chopé le truc le plus haut, on chope le truc le plus à gauche
            float X_min = float.MaxValue;
            for (int i = 0; i < piece.Length; i++)
            {
                if (piece[i].X < X_min) X_min = piece[i].X;
            }
            return new Vector2(X_min, Y_min);
        }

        int SeekMino(Vector2 mino, Vector2[] piece)
        {
            for (int i = 0; i < piece.Length; i++)
            {
                if (mino.X == piece[i].X && mino.Y == piece[i].Y) return i;
            }
            return -1;
        }

        int ConfirmOrientation(Vector2[] piece, int orientation)
        {
            int result = -1;
            bool vrai = false;

            for (int j = 0; j < 4; j++)
            {
                Vector2[] sample = new Vector2[BLOCKS_COUNT_IN_FIGURE];
                for (int i = 0; i < BLOCKS_COUNT_IN_FIGURE; i++) sample[i] = _figures[DynamicFigureNumber, j, i];
                Vector2 pos_ref = GetPositionOfPlacedPiece(sample);
                Vector2 pos = GetPositionOfPlacedPiece(piece);
                Vector2 offset = pos - pos_ref;

                bool pas_celui_la = false;
                for (int i = 0; i < BLOCKS_COUNT_IN_FIGURE; i++)
                {
                    if (SeekMino(sample[i] + offset, piece) < 0)
                    {
                        pas_celui_la = true;
                        break;
                    }
                }
                if (!pas_celui_la)
                {
                    if (orientation == j) vrai = true;
                    result = j;
                }
            }
            return vrai ? orientation : result;
        }

        void CopyBlock(int x_dest, int y_dest, int x_source, int y_source)
        {
            _boardFields[x_dest, y_dest] = _boardFields[x_source, y_source];
            _boardColor[x_dest, y_dest] = _boardColor[x_source, y_source];
        }

        public bool CollideWithNextDown(Vector2[] piece)
        {
            for (int i = 0; i < BLOCKS_COUNT_IN_FIGURE; i++)
            {
                if (piece[i].Y + 1 >= HEIGHT) return true;
                if (_boardFields[(int)piece[i].X, (int)piece[i].Y + 1] == FieldState.Static) return true;
            }
            return false;
        }

        public Vector2[] GetGhostPiece(Vector2[] piece)
        {
            Vector2[] result = new Vector2[BLOCKS_COUNT_IN_FIGURE];
            piece.CopyTo(result, 0);

            Vector2 decal = new Vector2(0, 1);

            while (!CollideWithNextDown(result))
            {
                for (int i = 0; i < BLOCKS_COUNT_IN_FIGURE; i++) result[i] += decal;
            }
            return result;
        }
        //*/


        void FixPieceAndSendEvent()
        {
            //On a posé la pièce!
            Debug.Assert(ConfirmOrientation(DynamicFigure, DynamicFigureModificationNumber) == DynamicFigureModificationNumber);
            Vector2 position = GetPositionOfPlacedPiece(DynamicFigure);
            byte orientation = (byte)DynamicFigureModificationNumber;
            uint pieceNumber = _piecesPlacedCount;
            //Send event to UI
            if (MoveExecuted != null) MoveExecuted(pieceNumber, orientation, (byte)position.X, (byte)position.Y);
            _piecesPlacedCount++;
        }

        public void MoveFigureDown()
        {
            // Sorting blocks fro dynamic figure to correct moving
            SortingVector2(ref DynamicFigure, false, DynamicFigure.GetLowerBound(0), 
                DynamicFigure.GetUpperBound(0));
            // Check colisions
            for (int i = 0; i < BLOCKS_COUNT_IN_FIGURE; i++)
            {
                if ((DynamicFigure[i].Y == HEIGHT - 1) ||
                    _boardFields[(int)DynamicFigure[i].X, (int)DynamicFigure[i].Y + 1] == FieldState.Static)
                {
                    for (int k = 0; k < BLOCKS_COUNT_IN_FIGURE; k++)
                        _boardFields[(int)DynamicFigure[k].X, (int)DynamicFigure[k].Y] = FieldState.Static;

                    FixPieceAndSendEvent();
                    showNewBlock = true;
                    return;
                }
            }
            // Move figure on board
            for (int i = BLOCKS_COUNT_IN_FIGURE - 1; i >= 0; i--)
            {
                _boardFields[(int)DynamicFigure[i].X, (int)DynamicFigure[i].Y + 1] = _boardFields[(int)DynamicFigure[i].X, (int)DynamicFigure[i].Y];
                _boardColor[(int)DynamicFigure[i].X, (int)DynamicFigure[i].Y + 1] = _boardColor[(int)DynamicFigure[i].X, (int)DynamicFigure[i].Y];
                ClearBoardField((int)DynamicFigure[i].X, (int)DynamicFigure[i].Y);
                // Change position for blocks in DynamicFigure
                DynamicFigure[i].Y = DynamicFigure[i].Y + 1;
            }
            // Change position vector
            PositionForDynamicFigure.Y++;
        }

        public void RotateFigureSRSTrueRotation(bool isClockWise)
        {
            Vector2[] test = new Vector2[BLOCKS_COUNT_IN_FIGURE];
            int rotation_1 = DynamicFigureModificationNumber;
            int rotation_2 = (DynamicFigureModificationNumber + (isClockWise ? 1 : -1)) % 4;
            //Calcul de l'offset
            Vector2 position = GetPositionOfSRSPiece(DynamicFigure);
            for (int i = 0; i < BLOCKS_COUNT_IN_FIGURE; i++) test[i] = _figures[DynamicFigureNumber, DynamicFigureModificationNumber, i];
            Vector2 offset = position - GetPositionOfSRSPiece(test);
            
            //Test successively all offset data
            Vector2[,] offset_data;
            if (DynamicFigureNumber == 0) offset_data = _i_offsetData;
            else if (DynamicFigureNumber == 3) offset_data = _o_offsetData;
            else offset_data = _jlstz_offsetData;

            bool success = false;
            for (int i = 0; i < 5; i++)
            {
                for (int j = 0; j < BLOCKS_COUNT_IN_FIGURE; j++)
                {
                    test[j] = offset + _figures[DynamicFigureNumber, rotation_2, j] + (offset_data[rotation_1, i] - offset_data[rotation_2, i]);
                }
                if (TryPlaceFigureOnBoard(test)) { success = true; break; }
            }
            if (success)
            {
                //Execute the rotation
                DynamicFigureModificationNumber = rotation_2;
                // Clear dynamic fields
                for (int i = 0; i <= DynamicFigure.GetUpperBound(0); i++)
                    ClearBoardField((int)DynamicFigure[i].X, (int)DynamicFigure[i].Y);
                DynamicFigure = test;
                for (int i = 0; i <= DynamicFigure.GetUpperBound(0); i++)
                {
                    _boardFields[(int)DynamicFigure[i].X, (int)DynamicFigure[i].Y] = FieldState.Dynamic;
                    _boardColor[(int)DynamicFigure[i].X, (int)DynamicFigure[i].Y] = DynamicFigureColor;
                }
            }
        }

        public void SortingVector2(ref Vector2[] vector, bool sortByX, int a, int b)
        {
            if (a >= b)
                return;
            int i = a;
            for (int j = a; j <= b; j++)
            {
                if (sortByX)
                {
                    if (vector[j].X <= vector[b].X)
                    {
                        Vector2 tempVector = vector[i];
                        vector[i] = vector[j];
                        vector[j] = tempVector;
                        i++;
                    }
                }
                else
                {
                    if (vector[j].Y <= vector[b].Y)
                    {
                        Vector2 tempVector = vector[i];
                        vector[i] = vector[j];
                        vector[j] = tempVector;
                        i++;
                    }
                }
            }
            int c = i - 1;
            SortingVector2(ref vector, sortByX, a, c - 1);
            SortingVector2(ref vector, sortByX, c + 1, b);
        }
        void ClearBoardField(int i, int j)
        {
            _boardFields[i, j] = FieldState.Free;
            _boardColor[i, j] = -1;
        }
        public override void Draw(GameTime gameTime)
        {
            Vector2 startPosition;
            // Draw the blocks
            for (int i = 0; i < WIDTH; i++)
                for (int j = 0; j < HEIGHT; j++)
                    if (_boardFields[i, j] != FieldState.Free)
                    {
                        startPosition = new Vector2((10 + i) * _rectangles[0].Width,
                            (2 + j) * _rectangles[0].Height);
                        sBatch.Draw(textures, startPosition, _rectangles[_boardColor[i, j]], Color.White);
                    }
            Vector2[] ghostPiece = GetGhostPiece(DynamicFigure);
            //Draw ghost piece
            for (int i = 0; i < BLOCKS_COUNT_IN_FIGURE; i++)
            {
                int x = (int)ghostPiece[i].X;
                int y = (int)ghostPiece[i].Y;
                if (_boardFields[x, y] == FieldState.Free)
                {
                    startPosition = new Vector2((10 + x) * _rectangles[0].Width,
                            (2 + y) * _rectangles[0].Height);
                    sBatch.Draw(textures, startPosition, _rectangles[_rectangles.Length - 2], Color.White);
                }
            }

            // Draw next figures
            Queue<byte>.Enumerator figure = _nextFigures.GetEnumerator();
            //Queue<int>.Enumerator modification = nextFiguresModification.GetEnumerator();
            int min = Math.Min(_nextFigures.Count, 4);
            for (int i = 0; i < min; i++)
            {
                figure.MoveNext();
                //modification.MoveNext();
                for (int j = 0; j < BLOCKS_COUNT_IN_FIGURE; j++)
                {
                    startPosition = _rectangles[0].Height * (new Vector2(24, 3 + 5 * i) +
                        _figures[figure.Current, 0, j]);
                    sBatch.Draw(textures, startPosition,
                        _rectangles[figure.Current], Color.White);
                }
            }

            base.Draw(gameTime);
        }
    }
}