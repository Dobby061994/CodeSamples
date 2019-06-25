using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class Agent : MonoBehaviour
{
	// Variables for our agent, 'Dungeon Dave':
	// The Q_table is a 2D matrix that will hold the reward values for each state and action Dave can take.
	public float[][] Q_table;
	// Each address in the Q_table matrix holds an actions array of each possible action Dave can take from that point:
	public float[] actions;
	// LearningRate affects how much Dave adjusts his Q_table values as he moves:
	// Epsilon is the value representing how likely Dave is to take a random action. The lower epsilon is, the more likely it is that he will take a path he knows to be rewarding:
	// CurrentQ represents the overall possible reward value for the location Dave is currently in:
	// Gamma represents how much Dave cares about future possible rewards. If gamma is 1, he will value all possible rewards equally, so gamma is kept at 0.99:
	// WaitTime dictates how long Dave will wait between taking each action. Lower values will make him move faster:
	public float learningRate, epsilon, reward, episodeReward, currentQ, epsilonMin, timeElapsed, waitTime, gamma;
	public int lastState, action, episodesElapsed, gridSize, stateSize, actionSize, stepsTaken, noOfFailures, noOfSuccesses;
	public bool epIsOver;
	Text curQText, epRewardText, epsilonText, failsText, successText;

	void Start()
	{
		BeginNewSession ();
		InitialiseQMatrix ();
	}

	void Update()
	{
		// Record the time that has passed since the last action Dave took:
		timeElapsed += Time.deltaTime;
		// If enough time has passed:
		if (timeElapsed > waitTime)
		{
			// If Dave has reached the step limit for the current episode:
			if (stepsTaken >= 100)
			{
				// End the episode, as Dave has failed to reach the goal:
				epIsOver = true;
				noOfFailures++;
			}
			// Else:
			else
			{
				// Dave takes a new action:
				Move (PickAction ());
			}
			// Dave updates his Q_table with his discovered rewards:
			UpdateQMatrix (DetermineState (), reward, epIsOver);
			// Dave starts counting the time that has passed since his lact action again:
			timeElapsed = 0;
		}

		// If Dave completes an episode, end that episode:
		if (epIsOver)
		{
			EndEpisode ();
		}

		// Update the UI text displays:
		curQText.text = "Current Q: " + currentQ.ToString();
		epRewardText.text = "Episode Reward: " + episodeReward.ToString();
		epsilonText.text = "Epsilon: " + epsilon.ToString();
		failsText.text = "Failures: " + noOfFailures.ToString ();
		successText.text = "Successes: " + noOfSuccesses.ToString ();
	}

	void BeginNewSession()
	{
		// Find the UI text displays:
		curQText = GameObject.Find ("CurQText").GetComponent<Text>();
		epRewardText = GameObject.Find ("EpRewardText").GetComponent<Text>();
		epsilonText = GameObject.Find ("EpsilonText").GetComponent<Text> ();
		failsText = GameObject.Find ("FailsText").GetComponent<Text> ();
		successText = GameObject.Find ("SuccessText").GetComponent<Text> ();
		// Initialise all the variables Dave will need to start Q learning:
		gridSize = 10;
		stateSize = gridSize * gridSize;
		actionSize = 4;
		reward = 0;
		episodeReward = 0;
		episodesElapsed = 0;
		epsilon = 1f;
		epsilonMin = 0.1f;
		gamma = 0.99f;
		stepsTaken = 0;
		noOfFailures = 0;
		noOfSuccesses = 0;
		epIsOver = false;
	}

	void EndEpisode()
	{
		// Whenever the current episode ends:
		// Dave returns to his starting position:
		transform.position = new Vector3 (0f, 0f, 0f);
		// Increment the total number of episodes elapsed:
		episodesElapsed++;
		// Set the episodeReward and stepsTaken back to 0 ready for the next episode:
		episodeReward = 0;
		stepsTaken = 0;
		epIsOver = false;
	}

	public void InitialiseQMatrix()
	{
		// Here Dave initialises every address in his Q_table to be zero:
		Q_table = new float[stateSize][];
		// For every state:
		for (int i = 0; i < stateSize; i++)
		{
			Q_table [i] = new float[actionSize];
			// And for every possible action from that state:
			for (int j = 0; j < actionSize; j++)
			{
				// Initialise the value as 0:
				Q_table [i] [j] = 0f;
			}
		}
	}

	List<float> DetermineState()
	{
		// Create a List of floats called 'state':
		List<float> state = new List<float> ();
		// Create a float called 'currentPoint' which represents Dave's current position on the grid:
		float currentPoint = (gridSize * transform.position.x) + transform.position.z;
		// Add 'currentPoint' to the list of floats:
		state.Add (currentPoint);
		// Return the list of floats:
		return state;
	}

	void UpdateQMatrix(List<float> state, float reward, bool epIsOver)
	{
		// Set 'nextState' as the first index of the 'state' list. NextState's value is Dave's current state as determined by the DetermineState() function:
		int nextState = Mathf.FloorToInt (state.First ());
		// If the episode is over:
		if (epIsOver)
		{
			// If Dave has reached this point, it means the last action he took resulted in the episode ending.
			// Dave adds his current reward value to the last state that he was in. If he has failed the episode for any reason, the reward value is likely to be quite low.
			// This means that the last state's total Q value will also end up quite low, signifying that it is a bad route to take that is more likely to end in a failure.
			// Alternatively, if Dave reached the goal, the reward value is likely to be higher.
			// This means that the last state's total Q value is likely to be noticeably higher than others, signifying that it is a good route to take that is more likely to lead to the goal.
			Q_table [lastState] [action] += learningRate * (reward - Q_table [lastState] [action]);
		}
		// Else if the episode is still ongoing:
		else
		{
			// For each state Dave moves through, he steadily decrements the reward as he goes. The longer an episode goes on, the lower its total reward will become.
			// This means that routes which do not end up leading Dave towards the goal will steadily have their Q values lowered, so Dave will start to favour exploring other routes.
			Q_table [lastState] [action] += learningRate * (reward + gamma * Q_table [nextState].Max () - Q_table [lastState] [action]);
		}
		// Dave updates what his last state was so he can keep adjusting his Q_table as he moves:
		lastState = nextState;
	}
		
	int PickAction()
	{
		// Dave looks at the Q_table and selects the action which has the best reward from his current state:
		action = Q_table [lastState].ToList ().IndexOf (Q_table [lastState].Max ());
		// Dave generates a random number between 0 and 1, and if that number is lower than the epsilon value, he will instead pick a random action:
		if (Random.Range(0f, 1f) < epsilon)
		{
			action = Random.Range(0, 4);
		}
		// Else, Dave sticks with what he believes to be the most rewarding action.
		// Each episode, decrease the epsilon from 1 to 0.1 over 2000 steps:
		if (epsilon > epsilonMin)
		{
			epsilon = epsilon - ((1f - epsilonMin) / (float)2000);
		}
		// CurrentQ represents the Q value of Dave's current location:
		currentQ = Q_table [lastState] [action];
		// Return the action that Dave has selected:
		return action;
	}

	void Move(int action)
	{
		// First, decrease the reward for each additional step Dave has taken this episode:
		reward = -0.05f;
		// Increment the number of steps Dave has taken:
		stepsTaken++;

		// Actions: 0 = forward, 1 = backward, 2 = left, 3 = right
		// Here Dave tests to see if his choice of action will make him hit a wall:
		// Dave does a physics overlap test which creates an array of all colliders located at the position he is about to go to.
		// Dave is looking for any object tagged "Wall". If he finds any, he then sorts them into a second array. If Dave finds none of these objects, the length of that second array will be 0, and therefore he can move:
		if (action == 3)
		{
			Collider[] wallTest = Physics.OverlapBox(new Vector3(transform.position.x + 1, 0, transform.position.z), new Vector3(0.3f, 0.3f, 0.3f));
			if (wallTest.Where (obj => obj.gameObject.tag == "Wall").ToArray ().Length == 0)
			{
				transform.position = new Vector3 (transform.position.x + 1, 0, transform.position.z);
			}
		}

		if (action == 2)
		{
			Collider[] wallTest = Physics.OverlapBox(new Vector3(transform.position.x - 1, 0, transform.position.z), new Vector3(0.3f, 0.3f, 0.3f));
			if (wallTest.Where (obj => obj.gameObject.tag == "Wall").ToArray ().Length == 0)
			{
				transform.position = new Vector3 (transform.position.x - 1, 0, transform.position.z);
			}
		}

		if (action == 1)
		{
			Collider[] wallTest = Physics.OverlapBox(new Vector3(transform.position.x, 0, transform.position.z + 1), new Vector3(0.3f, 0.3f, 0.3f));
			if (wallTest.Where (obj => obj.gameObject.tag == "Wall").ToArray ().Length == 0)
			{
				transform.position = new Vector3 (transform.position.x, 0, transform.position.z + 1);
			}
		}

		if (action == 0)
		{
			Collider[] wallTest = Physics.OverlapBox(new Vector3(transform.position.x, 0, transform.position.z - 1), new Vector3(0.3f, 0.3f, 0.3f));
			if (wallTest.Where (obj => obj.gameObject.tag == "Wall").ToArray ().Length == 0)
			{
				transform.position = new Vector3 (transform.position.x, 0, transform.position.z - 1);
			}
		}

		// Here Dave tests to see if after he has moved, he has hit an object:
		// Dave does another physics overlap test, this time at his current location.
		// Dave is looking for any object tagged either "goal" or "pit". If he finds either of these objects, he sorts them into a second array.
		// If the length of the second array is 1, it means Dave has either reached the goal or hit an obstacle, so Dave ends his current episode and adjusts his reward accordingly:
		Collider[] endTest = Physics.OverlapBox(transform.position, new Vector3(0.3f, 0.3f, 0.3f));
		if (endTest.Where (obj => obj.gameObject.tag == "Goal").ToArray ().Length == 1)
		{
			reward =1;
			epIsOver = true;
			noOfSuccesses++;
		}

		if (endTest.Where (obj => obj.gameObject.tag == "Obstacle").ToArray ().Length == 1)
		{
			reward = -1;
			epIsOver = true;
			noOfFailures++;
		}

		// Finally, Dave adds the reward value for each action to his total reward for the episode:
		episodeReward += reward;
	}
}