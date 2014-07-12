using UnityEngine;
using System.Collections;

public class CharacterSpawner : MonoBehaviour
{
	public GameObject characterPrf;

	public Rect spawnRange;

	void Start ()
	{

	}

	void Update ()
	{
		if (Game.Character().character == null) {
			var _character = Spawn();
			Game.Character().character = _character;
			
			_character.hitEnabled = false;
			_character.Invoke("EnableHit", 1.5f);
		}
	}
	
	Vector2 Locate() 
	{
		Vector2 _position = Vector2.zero;
		_position.x = transform.position.x + spawnRange.xMin + Random.Range(0, spawnRange.width);
		_position.y = transform.position.y + spawnRange.yMin + Random.Range(0, spawnRange.height);
		return _position;
	}

	Character Spawn() 
	{
		Vector3 _characterPosition = Locate();

		var _gameObj = GameObject.Instantiate(characterPrf, _characterPosition, Quaternion.identity) as GameObject;

		var _destroyable = _gameObj.GetComponent<Destroyable>();
		_destroyable.postDestroy += ListenDestroy;

		var _character = _gameObj.GetComponent<Character>();

		if (networkView.enabled && Network.peerType != NetworkPeerType.Disconnected)
		{
			_character.networkView.viewID = Network.AllocateViewID();
			_character.networkView.enabled = true;
			
			var _observed = _gameObj.AddComponent<InterpolatePosition>();
			_character.networkView.observed = _observed;
			
			networkView.RPC("SpawnUponServer", RPCMode.OthersBuffered, _character.networkView.viewID, _characterPosition);
		}

		return _character;
	}

	[RPC]
	void SpawnUponServer(NetworkViewID _viewID, Vector3 _position)
	{
		var _character = GameObject.Instantiate(characterPrf, _position, Quaternion.identity) as GameObject;

		_character.networkView.enabled = true;
		_character.networkView.viewID = _viewID;

		var _observed = _character.AddComponent<InterpolatePosition>();
		_character.networkView.observed = _observed;
	}

	void ListenDestroy(Destroyable _destroyable)
	{
		if (networkView.enabled) {
			Network.RemoveRPCs(networkView.viewID);
		}
	}
}
