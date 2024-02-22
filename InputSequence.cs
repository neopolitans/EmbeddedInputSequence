/*                                       Embedded Input Sequence by Alexander "maylizbeth" Gilbertson / B512325
 *                                        
 *                                    More information can be found in the GitHub. PROVIDED UNDER THE MIT LICENSE
 *                                                  https://github.com/neopolitans/EmbeddedInputSequence     
 *                                                  
 *------------------------------------------------------------------------------------------------------------------------------------------------------------
 *                                                         INPUT SEQUENCE MODULE for UNITY ENGINE
 *------------------------------------------------------------------------------------------------------------------------------------------------------------
 *
 *  An Optional Module which enables developers to create (and track) input sequences for events, behaviours and other invocable concepts/classes. This
 *  class only handles the input side, it is up to the developer to handle the behaviours triggered, which is done for the sake of being better able to
 *  decouple the module from any other classes. However, developers can also use the AbstractInputSequence<T> class to completely decouple this module from
 *  EmbeddedInputModule for a preferred input handler (e.g. Rewired, Unity.Input, InputSystem 1.7.0).
 *
 *
 *                         OPTIONAL FEATURES OF THIS MODULE REQUIRE EmbeddedInputModule AND UnityEngine.InputSystem package to work!
 *                                    More information can be found in the GitHub. PROVIDED UNDER THE MIT LICENSE
 *                                                  https://github.com/neopolitans/EmbeddedInputModule
 */

//------------------------------------------------------------------------------------------------------------------------------------------------------------
//                                                                 INPUT SEQUENCE | SETTINGS
//------------------------------------------------------------------------------------------------------------------------------------------------------------

  #define EIM_OPTMOD_InputSequence_DisableDebugMessages                       // Disables debug messages for Successful Inputs, Unsuccessful Inputs,
                                                                              // Sequenece completion, current indexes and next inputs for Dev Debugging.


//#define EIM_OPTMOD_InputSequence_DecoupleFromEmbeddedInputModule            // Masks all EmbeddedInputModule Dependent References and Classes.
                                                                              // Provided to make using AbstractInputSequence<T> with any preferred input
                                                                              // handler easier.

  #define EIM_OPTMOD_InputSequence_CheckGamepadInputsForKeyboardSequence      // Check gamepad inputs for each GamepadControl value in KeyboardSequence when
                                                                              // a non-sequential input is detected during UpdateSequence().

//------------------------------------------------------------------------------------------------------------------------------------------------------------

using System;
using UnityEngine;

#if !EIM_OPTMOD_InputSequence_DecoupleFromEmbeddedInputModule
using Input = EmbeddedInputModule;
using GamepadControl = EmbeddedInputModule.GamepadControl;
#endif

// Fun Fact: This entire module and concept *only* exists because I wanted to get an easter egg
//           of a character in my best friend's universe posed next to her motorbike which in
//           and of itself is a reference of a Sonic CD Easter Egg!

/// <summary>
/// An absrract class containing all members and virtual methods required within <see cref="InputSequence{T}"/>, without the generic typing.<br/>
/// Use this class for any collection and management for sets of sequences.
/// </summary>
public abstract class AbstractInputSequence
{
    /// <summary>
    /// The boolean values that are set as inputs are read sequentially. 
    /// </summary>
    public bool[] successfulInputs;

    /// <summary>
    /// Has the sequence been completed?
    /// </summary>
    public bool sequenceComplete = false;

    /// <summary>
    /// Should the sequence automatically reset if an unsuccessful input is detected? <br/>
    /// Accessibility Feature for players who may use special input devices and/or may struggle repeating sequences. <br/><br/>
    /// <b>Setting this to false should prevent any unsuccessful inputs detected in <see cref="UpdateSequence"/> from resetting the sequence. </b>
    /// </summary>
    public bool autoResetSequence = true;

    /// <summary>
    /// The current input as an index value that the sequence is listening for.
    /// </summary>
    public int current = 0;

    /// <summary>
    /// Was the last input in the sequence successfully detected?
    /// </summary>
    public bool lastInputSuccessful
    {
        get
        {
            if (current == 0) return false;
            else return this[current - 1];
        }
    }

    // Methods
    /// <summary>
    /// Update the sequence by listening to the player’s input. <br/>
    /// If the next input in the sequence is detected, the input after becomes the next input.
    /// </summary>
    public abstract void UpdateSequence();

    /// <summary>
    /// Mark the sequence as completed.
    /// </summary>
    public virtual void SetAsComplete()
    {
        sequenceComplete = true;
        #if !EIM_OPTMOD_InputSequence_DisableDebugMessages
        Debug.Log("Sequence Complete!");
        #endif
    }

    /// <summary>
    /// Reset the sequence. <br/>
    /// This will reset all values in the Successful Inputs array.
    /// </summary>
    public abstract void ResetSequence();

    // Custom Operators
    /// <summary>
    /// Indexer that reads the corresponding value from the Successful Inputs array.
    /// </summary>
    /// <param name="i"></param>
    /// <returns></returns>
    public virtual bool this[int i]
    {
        get { return successfulInputs[i]; }
        set { successfulInputs[i] = value; }
    }

    /// <summary>
    /// A custom true operator that returns if <see cref="sequenceComplete"/> is true.
    /// </summary>
    /// <param name="inputSeq">The Input Sequence to Navigate.</param>z
    /// <returns></returns>
    public static bool operator true(AbstractInputSequence inputSeq) => inputSeq.sequenceComplete;

    /// <summary>
    /// A custom false operator that returns if <see cref="sequenceComplete"/> is false.
    /// </summary>
    /// <param name="inputSeq">The Input Sequence to Navigate.</param>
    /// <returns></returns
    public static bool operator false(AbstractInputSequence inputSeq) => !inputSeq.sequenceComplete;
}

/// <summary>
/// An abstract class that can contain a set of <see cref="AbstractInputSequence"/> objects for tracking sequence inputs along multiple platforms. <br/>
/// Will also synchronise the completion state of all Input Sequences if one sequence is registered as complete. <br/><br/>
/// <b>Does not self-reset once complete. <br/> Does not reset any sequences if one sequence has a successful input.</b>
/// </summary>
public abstract class AbstractSequenceSet
{
    // - Attempts were made to synchronise sets, however, quickly abandoned because if sequences with different amounts of inputs were in a set, syncing would incorrectly
    //   mark the set as complete. If a sequence with less inputs was synced based on the state of a sequence with more inputs and that higher sequence had more successful
    //   inputs than the total amount of inputs in the lower sequence, it would false flag the lower sequence as complete and then the whole set is synced to that false flag.

    //   [!] This idea of syncing didn't make it past pseudocode. If you can find a non-volatile and watertight syncing method, feel free to leave an issue on the GitHub page. [!]

    /// <summary>
    /// The internal array of AbstractInputSequences 
    /// </summary>
    protected AbstractInputSequence[] sequences = new AbstractInputSequence[0];

    // Most performant and efficient to just keep this a variable member and sync it within UpdateSequence().
    /// <summary>
    /// Has a sequence in the set been marked completed.
    /// </summary>
    public bool sequenceComplete = false;

    // Methods

    /// <summary>
    /// Update all sequences in the set. <br/><br/>
    /// If the next input in a sequence is detected, the input after becomes the next input. <br/>
    /// Sequences may reset if Auto Reset Sequence is enabled per sequence.
    /// </summary>
    public virtual void UpdateSequence()
    {
        if (sequenceComplete || sequences.Length < 1) return;

        bool anySuccessfulInputOrIdle = false;
        bool anySequenceComplete = false;

        for (int i = 0; i < sequences.Length; i++)
        {
            // Cache the current value for each sequence.
            int oldCurrentValue = sequences[i].current;

            // Update each sequence.
            sequences[i].UpdateSequence();
            
            #if !EIM_OPTMOD_InputSequence_DisableDebugMessages
            Debug.Log(oldCurrentValue + " vs new value: " + sequences[i].current);
            #endif
            // If the current value has been updated, a successful input has occured.
            // Otherwise, if it's the same, then no input occured and the player's idle.
            if (sequences[i].current >= oldCurrentValue) anySuccessfulInputOrIdle = true;
            // If any sequence is complete, raise ASC Flag.
            if (sequences[i].sequenceComplete) anySequenceComplete = true;
        }

        // If *any* sequence is complete, sync all sequences.
        if (anySequenceComplete)
        {
            SetAsComplete();
            return;
        }

        if (!anySuccessfulInputOrIdle)
        {
            ResetSequence();
        }
    }

    /// <summary>
    /// Mark all sequences and the set as complete.
    /// </summary>
    public virtual void SetAsComplete()
    {
        // Fill all sequences' successful inputs with true values.
        // Then mark each sequence as complete.
        foreach (AbstractInputSequence seq in sequences)
        {
            for (int i = 0; i < seq.successfulInputs.Length; i++)
            {
                seq.successfulInputs[i] = true;
            }
            seq.sequenceComplete = true;
        }

        sequenceComplete = true;
        #if !EIM_OPTMOD_InputSequence_DisableDebugMessages
        Debug.Log("Sequence Complete!");
        #endif
    }

    /// <summary>
    /// Reset all sequences in the set and the set itself. <br/>
    /// Also resets each element in a sequence array.
    /// </summary>
    public virtual void ResetSequence()
    {
        sequenceComplete = false;
        foreach (AbstractInputSequence seq in sequences)
        {
            seq.ResetSequence();
        }
    }

    // Custom Operators
    /// <summary>
    /// Custom True-Operator to navigate through all input sequences and determine if all inputs have been successful.
    /// </summary>
    /// <param name="SequenceSet">The Sequence Set to Navigate.</param>
    /// <returns></returns>
    public static bool operator true(AbstractSequenceSet SequenceSet) => SequenceSet.sequenceComplete;

    /// <summary>
    /// Custom False-Operator to navigate through all input sequences and determine if any input has been unsuccessful.
    /// </summary>
    /// <param name="SequenceSet">The Sequence Set to Navigate.</param>
    /// <returns></returns
    public static bool operator false(AbstractSequenceSet SequenceSet) => !SequenceSet.sequenceComplete;
}

/// <summary>
/// A base object class that can be inherited from to create custom Input Sequences for preferred input handlers. <br/>
/// InputSequences track a sequence of player inputs and identifies when a specific sequence of inputs has occured. <br/><br/>
/// <b>Does not self-reset once complete.</b>
/// </summary>
public class InputSequence<T> 
    : AbstractInputSequence where T : Enum
{
    /// <summary>
    /// The array of controls to be correctly pressed or detected in sequential order.
    /// </summary>
    public T[] sequence;

    public override void UpdateSequence() { }

    public override void ResetSequence()
    {
        current = 0;
        sequenceComplete = false;

        // As successfulInputs is *always* created within the constructor, this should never be zero.
        if (sequence.Length > 0)
        {
            for (int i = 0; i < sequence.Length; i++)
            {
                successfulInputs[i] = false;
            }
        }
    }
}

#if !EIM_OPTMOD_InputSequence_DecoupleFromEmbeddedInputModule
/// <summary>
/// An object class that tracks player gamepad input and identifies when a sequence has been completed. <br/>
/// This is not a self-resetting class, call <see cref="AbstractInputSequence{T}.ResetSequence()"/> once the seuence has been marked as completed for reusability.
/// </summary>
public class GamepadSequence
    : InputSequence<GamepadControl>, IAccessibilityConfigurable
{
    // PUBLIC CONSTRUCTORS
    /// <summary>
    /// A constructor for GamepadSequence which takes any number of GamepadControl values.
    /// </summary>
    /// <param name="control"></param>
    public GamepadSequence(params GamepadControl[] controls)
    {
        sequence = controls;
        successfulInputs = new bool[controls.Length];
    }

    public bool IsAccessibilityEnabled => !autoResetSequence;

    // PUBLIC METHODS
    public override void UpdateSequence()
    {
        // If the sequence has been complete, return immediately.
        if (sequenceComplete) return;

        #if !EIM_OPTMOD_InputSequence_DisableDebugMessages
        Debug.Log($"Current Index: {current} | Current Key: {sequence[current]} | Next Key: {(current < successfulInputs.Length - 1 ? sequence[current + 1] : "Seq. Will Complete")}");
        #endif

        if (Input.GamepadControlToInput(sequence[current]))
        {
            #if !EIM_OPTMOD_InputSequence_DisableDebugMessages
            Debug.Log($"Input {Input.GamepadControlAsString(sequence[current])} successful!");
            #endif
            this[current] = true;
            if (current + 1 < sequence.Length) { current++; }
            else SetAsComplete();
        }
        else
        {
            if (autoResetSequence)
            {
                // I'll try <Enum.GetValues>, That's a Good Trick! - Anakin Skywalker, probably (not).

                // This is used as a shortform variation of the unrestricted anyKeyDown from EmbeddedInputModule.
                // Doing this saves lots of instructions by going through 13 of 14 buttons every frame instead of 509.
                foreach (GamepadControl key in Enum.GetValues(typeof(GamepadControl)))
                {
                    // Skip the current key from the list of checks, we've already checked it.
                    if (key == sequence[current]) continue;
                    else
                    {
                        if (Input.GamepadControlToInput(key))
                        {
                            #if !EIM_OPTMOD_InputSequence_DisableDebugMessages
                            Debug.Log($"Last Input {Input.GamepadControlAsString(key)} unsuccessful!\nSequence Reset!");
                            #endif
                            ResetSequence();
                        }
                    }
                }
            }
        }
    }

    public void SetAccessibility(bool setting) => autoResetSequence = setting;
}

/// <summary>
/// An object class that tracks player keyboard input and identifies when a sequence has been completed. <br/>
/// This is not a self-resetting class, call <see cref="AbstractInputSequence.ResetSequence()"/> once the seuence has been marked as completed for reusability.
/// </summary>
public class KeyboardSequence 
    : InputSequence<KeyCode>, IAccessibilityConfigurable
{
    public bool IsAccessibilityEnabled => !autoResetSequence;

    // PUBLIC CONSTRUCTORS
    /// <summary>
    /// A constructor for KeyboardSequence which takes any number of KeyCode values.
    /// </summary>
    /// <param name="control"></param>
    public KeyboardSequence(params KeyCode[] controls)
    {
        sequence = controls;
        successfulInputs = new bool[controls.Length];
    }

    #if EIM_OPTMOD_InputSequence_CheckGamepadInputsForKeyboardSequence
    /// <summary>
    /// An alternate constructor for KeyboardSequence which takes any number of GameControls. <br/>
    /// Leverages conversion methods found in EmbeddedInputModule to convert GameControl values to KeyCode values. <br/><br/>
    /// This constructor is only available if the preprocessor directive <b>&lt;EIM_OPTMOD_InputSequence_CheckGamepadInputsForKeyboardSequence&gt;</b> is uncommented.
    /// </summary>
    /// <param name="controls"></param>
    public KeyboardSequence(params GamepadControl[] controls)
    {
        sequence = new KeyCode[controls.Length];
        successfulInputs = new bool[controls.Length];

        // Convert gamepad controls to corresponding keycodes.
        for (int i = 0; i < controls.Length; i++)
        {
            sequence[i] = Input.GamepadControlToKeyCode(controls[i]);
        }
    }
    #endif

    // PUBLIC METHODS
    public override void UpdateSequence()
    {
        if (sequenceComplete) return;

        // As GetKeyDown is needed twice, this is cached once to prevent
        bool CurrentKey = Input.GetKeyDown(sequence[current]);

        #if !EIM_OPTMOD_InputSequence_DisableDebugMessages
        Debug.Log($"Current Index: {current} | Current Key: {sequence[current]} | Next Key: {(current < successfulInputs.Length - 1 ? sequence[current + 1] : "Seq. Will Complete")}");
        #endif

        if (CurrentKey)
        {
            #if !EIM_OPTMOD_InputSequence_DisableDebugMessages
            Debug.Log($"Input {sequence[current]} successful!");
            #endif
            this[current] = true;
            if (current + 1 < sequence.Length) { current++; }
            else SetAsComplete();
        }
        else
        {
            if (autoResetSequence)
            {
                // As KeyboardSequence relies on KeyCode and EmbeddedInputModule has helper methods for KeyCode values
                // more efficient checks can be applied and extended checks can be added via preprocessor directives.
                if (Input.anyKeyDown)
                {
                    #if !EIM_OPTMOD_InputSequence_DisableDebugMessages
                    Debug.Log($"Last Input {sequence[current]} unsuccessful!\nSequence Reset!");
                    #endif
                    ResetSequence();
                }
                #if EIM_OPTMOD_InputSequence_CheckGamepadInputsForKeyboardSequence
                else
                {
                    // For cases where developers have enabled GamepadControl value checking in Keyboard Sequence, and where
                    // GamepadControl values may be used with the alternate constructor, this is a more thorough input check prcoess.

                    foreach (GamepadControl key in Enum.GetValues(typeof(GamepadControl)))
                    {
                        // As this keycode could be a gamepad keycode, checking all gamepad controls is important.
                        // However, instead of filtering *all* gamepad keycodes, only filter ones that arent Start, Select or Platform Button.
                        if (sequence[current] == Input.GamepadControlToKeyCode(key)) continue;

                        if (Input.GamepadControlToInput(key) && autoResetSequence)
                        {
                            #if !EIM_OPTMOD_InputSequence_DisableDebugMessages
                            Debug.Log($"Last Input {sequence[current]} unsuccessful!\nSequence Reset!");
                            #endif
                            ResetSequence();
                        }
                    }
                }
                #endif
            }
        }
    }

    public void SetAccessibility(bool setting) => autoResetSequence = setting;
}

/// <summary>
/// A Sequence Set that encapsulates a Gamepad and Keyboard Sequence for multi-platform use.<br/>
/// This is not a self-resetting class, call <see cref="AbstractSequenceSet.ResetSequence()"/> once the set has been marked as completed for reusability.
/// </summary>
public class MultiplatformSequence 
    : AbstractSequenceSet, IAccessibilityConfigurable
{
    /// <summary>
    /// Construct a multi-platform Input Sequence Set. <br/>
    /// Takes any amount of constructors.
    /// </summary>
    /// <param name="paramSequences">A list of sequences to bundle in a set.</param>
    public MultiplatformSequence(params AbstractInputSequence[] inputSequences)
    {
        sequences = inputSequences;
    }

    public bool IsAccessibilityEnabled
    {
        get
        {
            // If there are no sequences in this set, return false.
            if (sequences.Length < 1) return false;

            // Assume true, until an sequence with autoResetSequence set to true is detected.
            // Then set to false, break and return. Otherwise, remains true.
            bool result = true;
            foreach (AbstractInputSequence seq in sequences)
            {
                if (seq.autoResetSequence)
                {
                    result = false;
                    break;
                }
            }
            return result;
        }

    }

    public void SetAccessibility(bool setting)
    {
        if (sequences.Length < 1) return;
        foreach (AbstractInputSequence seq in sequences)
        {
            seq.autoResetSequence = setting;
        }
    }
}

#endif