using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;

public class ColorChaserScript : MonoBehaviour
{

    public KMAudio audio;
    public KMBombInfo bomb;
    public KMSelectable[] buttons;

    public Material[] colorMats;
    public MeshRenderer[] moveDispRends;
    public MeshRenderer nextColRend;
    public MeshRenderer ledRend;

    List<int[]> colorMoves = new List<int[]>();
    List<int> collectOrder;
    private string[] colorNames = { "Red", "Yellow", "Blue", "Green", "Orange", "Purple" };
    private int[] colorPositions = new int[3];
    private int[] colorMoveIndexes = new int[3];
    private int curPos;
    private bool collect;
    private bool reverse;
    private bool animating;

    static int moduleIdCounter = 1;
    int moduleId;
    private bool moduleSolved;

    void Awake()
    {
        moduleId = moduleIdCounter++;
        foreach (KMSelectable obj in buttons)
        {
            KMSelectable pressed = obj;
            pressed.OnInteract += delegate () { PressButton(pressed); return false; };
        }
    }

    void Start()
    {
        curPos = UnityEngine.Random.Range(0, 16);
        Debug.LogFormat("[Color Chaser #{0}] Your starting cell: {1}", moduleId, curPos + 1);
        for (int i = 0; i < 3; i++)
        {
            regen:
            int[] moves = new int[3];
            for (int j = 0; j < 3; j++)
            {
                moves[j] = UnityEngine.Random.Range(-4, 5);
                while (moves[j] == 0)
                    moves[j] = UnityEngine.Random.Range(-4, 5);
            }
            for (int j = 0; j < colorMoves.Count; j++)
            {
                if (colorMoves[j].SequenceEqual(moves))
                    goto regen;
            }
            colorMoves.Add(moves);
            colorPositions[i] = UnityEngine.Random.Range(0, 16);
            Debug.LogFormat("[Color Chaser #{0}] {2}'s starting cell: {1}", moduleId, colorPositions[i] + 1, colorNames[i]);
            Debug.LogFormat("[Color Chaser #{0}] {1}'s moves: {2}, {3}, {4}", moduleId, colorNames[i], moves[0] > 0 ? "Forward " + moves[0] : "Backward " + Math.Abs(moves[0]), moves[1] > 0 ? "Forward " + moves[1] : "Backward " + Math.Abs(moves[1]), moves[2] > 0 ? "Forward " + moves[2] : "Backward " + Math.Abs(moves[2]));
        }
        collectOrder = new List<int>() { 0, 1, 2 }.Shuffle();
    }

    void PressButton(KMSelectable pressed)
    {
        if (moduleSolved != true && animating != true)
        {
            pressed.AddInteractionPunch();
            audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, pressed.transform);
            int index = Array.IndexOf(buttons, pressed);
            if (index == 0)
            {
                collect = true;
                nextColRend.material = colorMats[collectOrder[0] + 2];
            }
            else if (index == 1)
            {
                if (collect)
                {
                    collect = false;
                    collectOrder = new List<int>() { 0, 1, 2 }.Shuffle();
                }
                nextColRend.material = colorMats[0];
                audio.PlaySoundAtTransform("scan", transform);
                StartCoroutine(Scan());
            }
            else if (index == 2)
            {
                reverse = !reverse;
                if (reverse)
                    ledRend.material = colorMats[1];
                else
                    ledRend.material = colorMats[0];
            }
            else
            {
                Debug.LogFormat("[Color Chaser #{0}] You moved {1} cell{3} {2}.", moduleId, Math.Abs(index - 2), reverse ? "backward" : "forward", Math.Abs(index - 2) == 1 ? "" : "s");
                curPos = Mod(reverse ? curPos - (index - 2) : curPos + (index - 2), 16);
                for (int i = 0; i < 3; i++)
                {
                    if (collectOrder.Contains(i))
                    {
                        colorPositions[i] = Mod(colorMoves[i][colorMoveIndexes[i]] + colorPositions[i], 16);
                        colorMoveIndexes[i] = Mod(colorMoveIndexes[i] + 1, 3);
                    }
                    else
                        colorPositions[i] = curPos;
                }
                Debug.LogFormat("<Color Chaser #{0}> RYB color positions after move: {1}, {2}, {3}", moduleId, colorPositions[0], colorPositions[1], colorPositions[2]);
                if (collect)
                {
                    bool[] onYou = new bool[collectOrder.Count];
                    for (int i = 0; i < onYou.Length; i++)
                    {
                        if (colorPositions[collectOrder[i]] == curPos)
                            onYou[i] = true;
                    }
                    if (onYou.Count(x => x) == 1 && colorPositions[collectOrder[0]] == curPos)
                    {
                        collectOrder.RemoveAt(0);
                        if (collectOrder.Count == 0)
                        {
                            moduleSolved = true;
                            GetComponent<KMBombModule>().HandlePass();
                            nextColRend.material = colorMats[0];
                            ledRend.material = colorMats[0];
                            audio.PlaySoundAtTransform("win", transform);
                        }
                        else
                        {
                            nextColRend.material = colorMats[collectOrder[0] + 2];
                        }
                    }
                    else if (onYou.Count(x => x) > 1 && colorPositions[collectOrder[0]] == curPos)
                    {
                        Debug.LogFormat("[Color Chaser #{0}] Even though the target color landed on your cell, another did as well. Strike!", moduleId);
                        GetComponent<KMBombModule>().HandleStrike();
                    }
                    else if (onYou.Count(x => x) == 1 && colorPositions[collectOrder[0]] != curPos)
                    {
                        Debug.LogFormat("[Color Chaser #{0}] A color other than the target color landed on your cell. Strike!", moduleId);
                        GetComponent<KMBombModule>().HandleStrike();
                    }
                }
            }
        }
    }

    int ColorAtPosition(int pos)
    {
        bool red = false;
        bool yellow = false;
        bool blue = false;
        if (colorPositions[0] == pos)
            red = true;
        if (colorPositions[1] == pos)
            yellow = true;
        if (colorPositions[2] == pos)
            blue = true;
        if (red && !yellow && !blue)
            return 2;
        else if (!red && yellow && !blue)
            return 3;
        else if (!red && !yellow && blue)
            return 4;
        else if (red && yellow && !blue)
            return 6;
        else if (!red && yellow && blue)
            return 5;
        else if (red && !yellow && blue)
            return 7;
        else if (red && yellow && blue)
            return 8;
        else
            return 1;
    }

    int Mod(int x, int m)
    {
        int r = x % m;
        return r < 0 ? r + m : r;
    }

    IEnumerator Scan()
    {
        animating = true;
        for (int i = 0; i < 4; i++)
        {
            moveDispRends[i].material = colorMats[ColorAtPosition(Mod(reverse ? curPos - (i + 1) : curPos + i + 1, 16))];
            yield return new WaitForSeconds(.2f);
        }
        yield return new WaitForSeconds(.4f);
        for (int i = 0; i < 4; i++)
            moveDispRends[i].material = colorMats[0];
        animating = false;
    }

    //twitch plays
#pragma warning disable 414
    private readonly string TwitchHelpMessage = @"!{0} scan [Presses the scan button] | !{0} collect [Presses the collect button] | !{0} led [Presses the circular button] | !{0} 1/2/3/4 [Presses the specified display from bottom to top] | Commands are chainable with spaces";
#pragma warning restore 414
    IEnumerator ProcessTwitchCommand(string command)
    {
        string[] parameters = command.Split(' ');
        for (int i = 0; i < parameters.Length; i++)
        {
            if (!parameters[i].ToLowerInvariant().EqualsAny("scan", "collect", "led", "1", "2", "3", "4"))
            {
                yield return "sendtochaterror!f The specified command '" + parameters[i] + "' is invalid!";
                yield break;
            }
        }
        yield return null;
        for (int i = 0; i < parameters.Length; i++)
        {
            while (animating) yield return null;
            if (parameters[i].EqualsIgnoreCase("collect"))
                buttons[0].OnInteract();
            else if (parameters[i].EqualsIgnoreCase("scan"))
                buttons[1].OnInteract();
            else if (parameters[i].EqualsIgnoreCase("led"))
                buttons[2].OnInteract();
            else if (parameters[i].EqualsIgnoreCase("1"))
                buttons[3].OnInteract();
            else if (parameters[i].EqualsIgnoreCase("2"))
                buttons[4].OnInteract();
            else if (parameters[i].EqualsIgnoreCase("3"))
                buttons[5].OnInteract();
            else
                buttons[6].OnInteract();
            yield return new WaitForSeconds(.1f);
        }
    }

    // Autosolver by Quinn Wuest

    public class State
    {
        public int Red { get; private set; }
        public int Yellow { get; private set; }
        public int Blue { get; private set; }
        public int Current { get; private set; }
        public int MoveIx { get; private set; }

        public State(int r, int y, int b, int c, int m)
        {
            Red = r;
            Yellow = y;
            Blue = b;
            Current = c;
            MoveIx = m;
        }

        public override string ToString()
        {
            string temp = "";
            for (int i = 0; i < 16; i++)
            {
                string ch = "-";
                if (Red == i && Yellow == i & Blue == i)
                    ch = "n";
                else if (Red == i && Yellow == i)
                    ch = "o";
                else if (Red == i && Blue == i)
                    ch = "p";
                else if (Yellow == i && Blue == i)
                    ch = "g";
                else if (Red == i)
                    ch = "r";
                else if (Yellow == i)
                    ch = "y";
                else if (Blue == i)
                    ch = "b";
                if (Current == i && ch == "-")
                    ch = "#";
                else if (Current == i && ch != "-")
                    ch = ch.ToUpperInvariant();
                temp += ch;
            }
            return temp;
        }
    }

    struct QueueItem
    {
        public State State { get; private set; }
        public State Parent { get; private set; }
        public int MoveAmount { get; private set; }
        public QueueItem(State state, State parent, int amount)
        {
            State = state;
            Parent = parent;
            MoveAmount = amount;
        }
    }

    private State GetNewState(State state, int moveAmt)
    {
        int rIx = state.Red;
        int yIx = state.Yellow;
        int bIx = state.Blue;
        int cIx = state.Current;
        int mIx = state.MoveIx;

        rIx = Mod(colorMoves[0][mIx] + rIx, 16);
        yIx = Mod(colorMoves[1][mIx] + yIx, 16);
        bIx = Mod(colorMoves[2][mIx] + bIx, 16);
        cIx = Mod(cIx + moveAmt, 16);
        mIx = Mod(mIx + 1, 3);

        return new State(rIx, yIx, bIx, cIx, mIx);
    }

    private int CheckForColor(State state)
    {
        if (state.Current == state.Red && state.Current == state.Yellow && state.Current == state.Blue)
            return 6;
        if (state.Current == state.Red && state.Current == state.Yellow)
            return 3;
        if (state.Current == state.Red && state.Current == state.Blue)
            return 4;
        if (state.Current == state.Yellow && state.Current == state.Blue)
            return 5;
        if (state.Current == state.Red)
            return 0;
        if (state.Current == state.Yellow)
            return 1;
        if (state.Current == state.Blue)
            return 2;
        return -1;
    }

    private IEnumerator TwitchHandleForcedSolve()
    {
        while (collectOrder.Count != 0)
        {
            var goal = collectOrder.First();
            var currentState = new State(colorPositions[0], colorPositions[1], colorPositions[2], curPos, colorMoveIndexes[goal]);
            var path = FindPath(currentState, goal);
            while (animating)
                yield return true;
            if (!collect)
            {
                buttons[0].OnInteract();
                yield return new WaitForSeconds(0.1f);
            }
            for (int i = 0; i < path.Count; i++)
            {
                int mvt = path[i];
                if ((mvt < 0 && !reverse) || (mvt > 0 && reverse))
                {
                    buttons[2].OnInteract();
                    yield return new WaitForSeconds(0.1f);
                }
                mvt = Math.Abs(mvt);
                buttons[mvt + 2].OnInteract();
                yield return new WaitForSeconds(0.1f);
            }
        }
        yield break;
    }

    private List<int> FindPath(State state, int goalColor)
    {
        var visited = new Dictionary<State, QueueItem>();
        var q = new Queue<QueueItem>();
        q.Enqueue(new QueueItem(state, null, 0));
        State solutionState = null;
        int ix = 0;
        while (q.Count > 0)
        {
            var qi = q.Dequeue();
            if (visited.ContainsKey(qi.State))
                continue;
            visited[qi.State] = qi;
            if (CheckForColor(qi.State) == goalColor && ix != 0)
            {
                solutionState = qi.State;
                break;
            }
            var arr = new[] { 1, 2, 3, 4, -1, -2, -3, -4 };
            for (int i = 0; i < arr.Length; i++)
                if (new[] { -1, goalColor }.Contains(CheckForColor(GetNewState(qi.State, arr[i]))))
                    q.Enqueue(new QueueItem(GetNewState(qi.State, arr[i]), qi.State, arr[i]));
            ix++;
        }
        var r = solutionState;
        var path = new List<int>();
        while (true)
        {
            var nr = visited[r];
            if (nr.Parent == null)
                break;
            path.Add(nr.MoveAmount);
            r = nr.Parent;
        }
        path.Reverse();
        return path;
    }
}