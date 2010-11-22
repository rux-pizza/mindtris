package Server;

import Util.*;

import java.io.*;
import java.net.*;
import java.security.*;
import java.util.*;
import java.util.Map.Entry;
import java.sql.*;

import javax.crypto.BadPaddingException;
import javax.crypto.Cipher;
import javax.crypto.IllegalBlockSizeException;
import javax.crypto.NoSuchPaddingException;
import javax.crypto.ShortBufferException;

public class Server extends Thread {
	public static final short PORT = 1337+42;
	private static final boolean DEBUG = true;
	private static final short KEY_LEN = 1024;
	private static String HELLO_MSG = "Welcome to MindTris Server\n";
	
	//private HashMap<Channel, PeerInfo> _clients;
	//private RSAPublicKeySpec _publicKey;
	private PublicKey _publicKey;
	private Cipher _decrypter;
	private UsrDataBase _db;
	private IdMap<Lobby> _lobbies;
	private Random _rdmGen;
	
	public Server () {
		_db = new UsrDataBase();
		_lobbies = new IdMap<Lobby>();

		/* generate rsa keys */
		try {
			_rdmGen = SecureRandom.getInstance ("SHA1PRNG");
	//		KeyFactory fact = KeyFactory.getInstance("RSA");
			KeyPairGenerator gen = KeyPairGenerator.getInstance("RSA");
			gen.initialize(KEY_LEN);
			KeyPair keyPair = gen.generateKeyPair();
	//		_publicKey = fact.getKeySpec(keyPair.getPublic(), RSAPublicKeySpec.class);
			_publicKey = keyPair.getPublic();
						
			_decrypter = Cipher.getInstance(Channel.CRYPT_SCHEME);
			_decrypter.init(Cipher.DECRYPT_MODE, keyPair.getPrivate());
		} catch (NoSuchAlgorithmException e) {
			e.printStackTrace();
		} catch (InvalidKeyException e) {
			e.printStackTrace();
		} catch (NoSuchPaddingException e) {
			e.printStackTrace();
	//	} catch (InvalidKeySpecException e) {
	//		e.printStackTrace();
		}

	/*	byte[] n1 = new byte[8];
		byte[] n2 = new byte[8];
		_rdmGen.nextBytes(n1);
		_rdmGen.nextBytes(n2);
		_lobbies.add(new Lobby(1, "lobby test 1".getBytes(), n1, (byte)0, (byte)10, "pass".getBytes(), "kikoO".getBytes()));
		Lobby test = new Lobby(2, "lobby test number 2".getBytes(), n2, (byte)2, (byte)5, null, "lolzorz".getBytes());
		test._peers.add(new Peer("kikoO".getBytes(), new byte[]{1,0,0,1}, 1000, "key1".getBytes()));
		test._peers.add(new Peer("roXoR".getBytes(), new byte[]{1,0,0,2}, 1001, "key2".getBytes()));
		_lobbies.add(test);
	/**/	
		
		
		/* connect to the sql db *
	    try {
	    	String driverName = "org.gjt.mm.mysql.Driver";
			Class.forName(driverName);
		    String url = "jdbc:mysql://" + serverName +  "/" + mydatabase; // a JDBC url

		} catch (ClassNotFoundException e1) {
			e1.printStackTrace();
		}
		
		Connexion connection = DriverManager.getConnection(url, username, password);
	    
	    /* */
	}
	
	public void run () {
		try {
			ServerSocket srv = new ServerSocket(PORT);
			synchronized(this) {
				notifyAll();
			}
			System.out.println("Server launched");
			
			while( true ) {
				Thread.sleep(10);
				
				Socket skt = srv.accept();
				CltSrvCh ch = new CltSrvCh(skt);
				MyHandler hdl = new MyHandler(ch);
				
				debug("Wait hello message from client " + skt.getRemoteSocketAddress());
				
				hdl.start();
			}
		} catch(Exception e) {
			System.out.println("Server error");
			e.printStackTrace();
		}
	}
	
	private byte [] decrypt ( byte [] crypt ) {
		try {
			return _decrypter.doFinal(crypt);
		} catch (IllegalBlockSizeException e) {
			// TODO Auto-generated catch block
			e.printStackTrace();
		} catch (BadPaddingException e) {
			// TODO Auto-generated catch block
			e.printStackTrace();
		}
		
		return null;
	}
	
	@SuppressWarnings("unused")
	private void debug ( byte[]... raw ) {
		if( DEBUG )
			for( byte[] b : raw )
				System.out.println(Channel.bytes2string(b));
	}
	private void debug ( String m ) {
		if( DEBUG ) System.out.println(m);
	}
	
	///////// HANDLERS /////////
	private class MyHandler extends MsgHandler<CltSrvCh> {
		public MyHandler(CltSrvCh ch) {
			super(ch);
			
			addHdl(Msg.C_HELLO, new HelloHdl());
			addHdl(Msg.CREATE_USER, new UsrCreateHdl());
			addHdl(Msg.LOGIN, new LoginHdl());
			addHdl(Msg.CREATE_LOBBY, new LobbyCreateHdl());
			addHdl(Msg.GET_LOBBY_LIST, new LobbyListHdl());
			addHdl(Msg.JOIN_LOBBY, new LobbyJoinHdl());
		}
	}
	
	private class HelloHdl implements Handler<CltSrvCh> {
		public void handle(byte[] data, CltSrvCh ch) throws IOException {
			if( Arrays.equals(data, Channel.protocolVersion) ) {
				debug("Send hello message");
				
				byte[] encodedKey = _publicKey.getEncoded();
				short len = (short) encodedKey.length;
				ch.send(Msg.S_HELLO, 0x00, Channel.short2bytes(len), encodedKey, HELLO_MSG.getBytes(Channel.ENCODING));
			} else {
				debug("Wrong protocol version");
				ch.send(Msg.S_HELLO, new byte[]{0x01, 0x00}, HELLO_MSG.getBytes(Channel.ENCODING));
			}
		}
	}
	
	private class UsrCreateHdl implements Handler<CltSrvCh> {
		public void handle(byte[] data, CltSrvCh ch) throws IOException {
			User usr = new User (decrypt(data));

			byte ans;
			
			if( ! usr.isNameValid() ) ans = 0x02;
			else if( ! usr.isPwdValid() ) ans = 0x03;
			else if( ! usr.isEmailValid() ) ans = 0x04;
			else {
				try {
					_db.add(usr);
					ans = 0x00;
				} catch (UserAlreadyExists e) {
					ans = 0x01;
				}
			}
			
			ch.send(Msg.USR_CREATION, ans);
		}
	}
	
	private class LoginHdl implements Handler<CltSrvCh> {
		public void handle(byte[] data, CltSrvCh ch) throws IOException {
			byte[] loginInfo = decrypt(data);
				
			byte usrNameLen = loginInfo[0],
				 pwdLen = loginInfo[1+usrNameLen];
			byte [] usrName = new byte [usrNameLen];
			byte [] pwd = new byte [pwdLen];
			
			System.arraycopy(loginInfo, 1, usrName, 0, usrNameLen);
			System.arraycopy(loginInfo, 1+usrNameLen+1, pwd, 0, pwdLen);
			
			byte ans;
			byte displayName [] = {};
			
			try {
				User usr = _db.getInfos(new String(usrName, Channel.ENCODING));
				ch.setUsr(usr);
				
				if( Arrays.equals(usr._pwd, pwd) ) {
					ans = 0x00;
					
					byte dispNameLen = (byte) usr._displayName.length;
					displayName = new byte [2+dispNameLen];
					displayName[0] = dispNameLen;
					System.arraycopy(usr._displayName, 0, displayName, 1, dispNameLen);
				} else {
					ans = 0x02;
				}
			} catch ( UserDoesntExists e ) {
				ans = 0x01;
			}
			
			ch.send(Msg.LOGIN_REPLY, ans, displayName);
		}
	}
	
	private class LobbyCreateHdl implements Handler<CltSrvCh> {
		public void handle(byte[] data, CltSrvCh ch) throws IOException {
			int offset = 0;
			byte nameLen = data[offset++];
			byte[] name = new byte[nameLen];
			System.arraycopy(data, offset, name, 0, nameLen);
			offset += nameLen;
			byte maxPlayers = data[offset++];
			boolean pwdRequired = Channel.byte2bool(data[offset++]);
			byte [] pwd;
			
			byte[] creator = ch.getUsr()._displayName;
			
			if( pwdRequired ) {
				int pwdCryptLen = data.length - offset;
				byte[] pwdCrypt = new byte[pwdCryptLen];
				System.arraycopy(data, offset, pwdCrypt, 0, pwdCryptLen);
				byte[] pwdBuf = decrypt(pwdCrypt);
				int pwdLen = pwdBuf[0];
				pwd = new byte[pwdLen];
				System.arraycopy(pwdBuf, 1, pwd, 0, pwdLen);
				
				// TODO check pwd
				if( pwd == null ) {
					ch.send(Msg.LOBBY_CREATED, 0x01);
					return;
				}
			} else {
				pwd = null;
			}

			int id = _lobbies.getNextId();
			byte[] nonce = new byte[8];
			_rdmGen.nextBytes(nonce);
			_lobbies.add(new Lobby(id, name, nonce, (byte)0, maxPlayers, pwd, creator));
			
			ch.send(Msg.LOBBY_CREATED, 0x00, Channel.int2bytes(id), nonce);
		}
		
	}
	
	private class LobbyListHdl implements Handler<CltSrvCh> {
		public void handle(byte[] _, CltSrvCh ch) throws IOException {
			ch.send(Msg.LOBBY_LIST, Lobby.listToBytes(_lobbies));
		}
	}
	
	private class LobbyJoinHdl implements Handler<CltSrvCh> {
		public void handle(byte[] data, CltSrvCh ch) throws IOException {
			int offset = 0;
			int id = Channel.bytes2int(data, offset);
			offset += 4;
			int port = Channel.bytes2short(data, offset);
			offset += 2;
			int keyLen = Channel.bytes2short(data, offset);
			offset += 2;
			byte[] encodedKey = new byte[keyLen];
			System.arraycopy(data, offset, encodedKey, 0, keyLen);
			offset += keyLen;
			int pwdLen = data.length - offset;
			byte[] encryptedPwd = new byte[pwdLen];
			System.arraycopy(data, offset, encryptedPwd, 0, pwdLen);
			
			Lobby l = _lobbies.get(id);
			
			if( ! l.pwdRequired() || Arrays.equals(decrypt(encryptedPwd), l._pwd) ) {
				if( l._nbPlayers < l._maxPlayers ) {
					byte peerId = (byte) _lobbies.getNextId();
					
					ch.send(Msg.JOINED_LOBBY, 0x00, l.toBytes(peerId));
					
					User usr = ch.getUsr();
					Peer peer = new Peer(usr._displayName, ch.getIp(), port, encodedKey);
					
					for( Entry<Integer, Peer> o : l._peers ) {
						Peer p = o.getValue();
						// TODO /!\ peerId <-> status_update
						p.getCh().send(Msg.UPDATE_CLIENT, 0x00, p.toBytes());
					}
					
					
					l._peers.add(peer);
					
					return;
				} else {
					ch.send(Msg.JOINED_LOBBY, 0x02);
				}
			} else {
				ch.send(Msg.JOINED_LOBBY, 0x01);
			}
		}
	}
	
	
/*	private class TestChatListHandler implements Handler {
		public void handle(byte[] data, Channel ch) throws IOException {
			debug("Sending list of peers");
			byte [][] list = new byte[_clients.size()][];
			int i=0;
			
			Enumeration<PeerInfo> peers = _clients.elements(); 
			while( peers.hasMoreElements() ) {
				list[i++] = peers.nextElement().toBytes();
			}
			
			ch.write(new Msg(Msg.TEST_CHAT_LIST_PEERS, list));
			PeerInfo peer = PeerInfo.peerFromBytes(data);
			addPeer(ch, peer);
		}
	}*/
}