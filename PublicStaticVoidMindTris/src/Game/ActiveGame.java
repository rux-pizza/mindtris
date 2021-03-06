package Game;

import java.awt.event.ActionEvent;
import java.awt.event.ActionListener;
import java.io.IOException;
import java.util.LinkedList;
import java.util.List;

import javax.swing.Timer;

import Client.Client;
import Gui.MainWindow;

public class ActiveGame extends Game {

	////// FIELDS //////
	private Client 			_c;
	protected MainWindow	_gui;
	private int				_fallX,
							_fallY,
							_fallenY;
	private List<Move>		_moves;
	private Timer			_timer;
	
	////// CONSTRUCTORS //////
	public ActiveGame ( Client c, int seed ) {
		super(seed);
		
		_c = c;
		_moves = new LinkedList<Move>();
	}
	
	////// PUBLIC METHODS //////
	public void setGui ( MainWindow w ) {
		_gui = w;
	}
	
	public void leftMove() {
		if( !_currentPiece.collide(_board, _fallX-1, _fallY) ) {
			_fallX--;
			computeFall();
		}
	}

	public void rightMove () {
		if( !_currentPiece.collide(_board, _fallX+1, _fallY) ) {
			_fallX++;
			computeFall();
		}
	}

	public void rotate() {
		Kick k = _currentPiece.rotate(_board, _fallX, _fallY);
		if( k != null ) {
			_fallX += k._i;
			_fallY += k._j;
			computeFall();	
		}
	}
	
	public void hardDrop() throws IOException {
		placeCurrent(_fallX, _fallenY);
	}

	public void softDrop() throws IOException {
		if( _currentPiece.collide(_board, _fallX, _fallY-1) ) placeCurrent(_fallX, _fallY);
		else _fallY--;
	}
	
	public List<Move> getMoves() {
		List<Move> ret = _moves;
		
		_moves = new LinkedList<Move>();
		
		return ret;
	}

	////// OVERRIDE //////
	public void start() throws IOException {
		super.start();
		
		nextFall();
		
		_timer = new Timer(1000, new ActionListener() {
			public void actionPerformed(ActionEvent ev) {
				try {
					if( _currentPiece.collide(_board, _fallX, _fallY-1) ) {
						placeCurrent(_fallX, _fallY);
					} else {
						_fallY--;
					}
					_display.repaint();
				} catch (IOException e) {
					e.printStackTrace();
				}
			}
		});
		
		_timer.start();
	}
	
	public int checkLines ( int yHigh ) {
		int nbLines = super.checkLines(yHigh);
		
		if( nbLines > 0 ) _gui.addScore(nbLines);
		
		return nbLines;
	}
	
	public void GameOver () {
		super.gameOver();
		_timer.stop();
		_gui.gameOver();
	}

	////// GETTER //////
	public int x () {
		return _fallX;
	}
	public int y () {
		return _fallY;
	}
	public int fallen () {
		return _fallenY;
	}

	
	////// PRIVATE METHODS //////
	protected Piece getNextPiece () throws IOException {
		if( _pieces.size() <= 7 ) _c.askForNewPieces();
		
		return super.getNextPiece();
	}
	private void computeFall () {
		_fallenY = _fallY;
		
		while( !_currentPiece.collide(_board, _fallX, _fallenY-1) )
			_fallenY--;
	}
	
	private void nextFall () throws IOException {
		_currentPiece = getNextPiece();
		
		_fallY = Game.H + _currentPiece.spawnY();
		_fallX = Game.W/2 + _currentPiece.spawnX();
		
		_gui.upNextPieces();
		_display.repaint();
		computeFall();
	}
	
	private void placeCurrent ( int x, int y ) throws IOException {
		if( y >= H ) {
			gameOver();
			return;
		}
		
		placePiece(_currentPiece, x, y, _roundNb);
		
		// TODO check
		_moves.add(new Move(_pieceNb, _currentPiece.getRotation(),
							x+_currentPiece.offsetX(), y-_currentPiece.offsetY()));

		nextFall();
	}
}
