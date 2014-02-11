using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Input;
using System.Globalization;

namespace PictureSOMWindow
{
    public enum SOMVisualType
    { 
        COMPETITION_LAYER_MAP,
        COMPETITION_LAYER_MAP_FIXED,
        ERROR_MAP
    }

    public class SOMVisual : FrameworkElement
    {
        // Create a collection of child visual objects.
        private VisualCollection _children;
        private int _border = 5;
        private int _boardWidthFactor;
        private int _boardHeightFactor;
        private int _boardSize;
        private PictureSOM.SOM _som;
        private SOMVisualType _visualType;
        private DrawingVisual _drawingVisual;
        DrawingVisual _boardDrawingVisual;

        public SOMVisualType VisualType { get { return _visualType; } }

        public int BorderSize { get { return _border; } }

        public SOMVisual(SOMVisualType type, int size, int width, int height, PictureSOM.SOM s)
        {
            _visualType = type;
            _boardSize = PictureSOM.SOMConstants.NUM_NODES_ACROSS; // assuming that across and down are the same
            _boardWidthFactor = (width - (2 * _border)) / size;
            _boardHeightFactor = (height - (2 * _border)) / size;

            _som = s;

            _children = new VisualCollection(this);
            DrawBoard();
            _children.Add(_boardDrawingVisual); // Render the grid

            if (_visualType == SOMVisualType.COMPETITION_LAYER_MAP)
            {
                CreateDrawingVisual_Text(s);
            }
            else if(_visualType == SOMVisualType.COMPETITION_LAYER_MAP_FIXED)
            {
                CreateDrawingVisual_Text_Fixed(s);
            }
            else if (_visualType == SOMVisualType.ERROR_MAP)
            {
                CreateDrawingVisual_ErrorMap(s);
            }
            _children.Add(_drawingVisual);

            // Add the event handler for MouseLeftButtonUp.
            this.MouseLeftButtonUp += new MouseButtonEventHandler(SOMVisual_MouseLeftButtonUp);
        }

        public void DrawBoard()
        {
            if(_boardDrawingVisual == null)
            {
                _boardDrawingVisual = new DrawingVisual();            
            }

            Brush boardBrush = new SolidColorBrush(Colors.White);
            Pen blackPen = new Pen(Brushes.Black, 0.1);

            DrawingContext dc = _boardDrawingVisual.RenderOpen();

            dc.DrawRectangle(boardBrush, new Pen(Brushes.Black, 0.2), new Rect(0, 0, _boardSize * _boardWidthFactor + _border * 2, _boardSize * _boardHeightFactor + _border * 2));
            dc.DrawRectangle(boardBrush, new Pen(Brushes.Black, 0.2), new Rect(_border, _border, _boardSize * _boardWidthFactor, _boardSize * _boardHeightFactor));

            for (int x = 0; x < _boardSize; x++)
            {
                for (int y = 0; y < _boardSize; y++)
                {
                    dc.DrawRectangle(null, blackPen, new Rect(GetPosX(x), GetPosY(y), _boardWidthFactor, _boardHeightFactor));
                }
            }
            dc.Close();
        }

        public void HighLightCell(int x, int y)
        {
            using (DrawingContext dc = _boardDrawingVisual.RenderOpen())
            {
                dc.DrawRectangle(Brushes.Red, null, new Rect(GetPosX(x), GetPosY(y), _boardWidthFactor, _boardHeightFactor));
            }
        }

        public void CreateDrawingVisual_Text(PictureSOM.SOM _som)
        {
            this._som = _som;
            // Create an instance of a DrawingVisual.
            if (_drawingVisual == null)
            {
                _drawingVisual = new DrawingVisual();
            }

            // Retrieve the DrawingContext from the DrawingVisual.
            using (DrawingContext dc = _drawingVisual.RenderOpen())
            {
                Typeface font = new Typeface("Verdana");
                int offsetX = Convert.ToInt32(_boardWidthFactor * .5) - 5;
                int offsetY = Convert.ToInt32(_boardHeightFactor * .5) - 5;
                for (int x = 0; x < _boardSize; x++)
                {
                    for (int y = 0; y < _boardSize; y++)
                    {
                        dc.DrawText(
                            new FormattedText(
                                _som.CompetitionLayer[x, y].GetImageCount().ToString(),
                                CultureInfo.GetCultureInfo("en-us"),
                                FlowDirection.LeftToRight,
                                font, 12, Brushes.Black),
                                new Point(GetPosX(x) + offsetX, GetPosY(y) + offsetY));
                    }
                }
            }
            // Close the DrawingContext to persist changes to the DrawingVisual.
            //dc.Close();
        }

        public void CreateDrawingVisual_Text_Fixed(PictureSOM.SOM _som)
        {
            this._som = _som;
            // Create an instance of a DrawingVisual.
            if (_drawingVisual == null)
            {
                _drawingVisual = new DrawingVisual();
            }

            // Retrieve the DrawingContext from the DrawingVisual.
            DrawingContext dc = _drawingVisual.RenderOpen();
            Typeface font = new Typeface("Verdana");
            int offsetX = Convert.ToInt32(_boardWidthFactor * .5) - 5;
            int offsetY = Convert.ToInt32(_boardHeightFactor * .5) - 5;
            for (int x = 0; x < _boardSize; x++)
            {
                for (int y = 0; y < _boardSize; y++)
                {
                    dc.DrawText(
                        new FormattedText(
                            _som.CompetitionLayer[x, y].GetImageCount().ToString(),
                            CultureInfo.GetCultureInfo("en-us"),
                            FlowDirection.LeftToRight,
                            font, 12, Brushes.Black),
                            new Point(GetPosX(x) + offsetX, GetPosY(y) + offsetY));
                }
            }
            dc.Close();
        }

        public void CreateDrawingVisual_ErrorMap(PictureSOM.SOM _som)
        {
            this._som = _som;
            // Create an instance of a DrawingVisual.
            if (_drawingVisual == null)
            {
                _drawingVisual = new DrawingVisual();
            }

            // Retrieve the DrawingContext from the DrawingVisual.
            using (DrawingContext dc = _drawingVisual.RenderOpen())
            {
                for (int x = 0; x < _boardSize; x++)
                {
                    for (int y = 0; y < _boardSize; y++)
                    {
                        float[] weight = new float[] { _som.ErrorMap[x, y, 0], _som.ErrorMap[x, y, 1], _som.ErrorMap[x, y, 2] };
                        SolidColorBrush errorBrush = new SolidColorBrush(Color.FromRgb(
                            PictureSOM.SOMHelper.Convert_FloatToByte(weight[0]),
                            PictureSOM.SOMHelper.Convert_FloatToByte(weight[1]),
                            PictureSOM.SOMHelper.Convert_FloatToByte(weight[2])));
                        dc.DrawRectangle(errorBrush, null, new Rect(GetPosX(x), GetPosY(y), _boardWidthFactor, _boardHeightFactor));
                    }
                }
            }
            // Close the DrawingContext to persist changes to the DrawingVisual.
            //dc.Close();
        }

        private double GetPosX(double value)
        {
            double result = (_boardWidthFactor * value) + _border;
            return result;
        }

        private double GetPosY(double value)
        {
            double result = _boardHeightFactor * value + _border;
            return result;
        }

        // Capture the mouse event and hit test the coordinate point value against
        // the child visual objects.
        void SOMVisual_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            // Retreive the coordinates of the mouse button event.
            System.Windows.Point pt = e.GetPosition((UIElement)sender);
        }

        // Provide a required override for the VisualChildrenCount property.
        protected override int VisualChildrenCount
        {
            get { return _children.Count; }
        }

        // Provide a required override for the GetVisualChild method.
        protected override Visual GetVisualChild(int index)
        {
            if (index < 0 || index >= _children.Count)
            {
                throw new ArgumentOutOfRangeException();
            }

            return _children[index];
        }
    }
}
