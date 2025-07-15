Pacman-Clone
============

![Screenshot](http://i.imgur.com/GQcmfQY.png)

**[Play on Web!](http://vilbeyli.github.io/Pacman/)**

A Pacman clone made in Unity3D, with tutorials from [noobtuts](http://noobtuts.com/unity/2d-pacman-game). In addition to the tutorial, [the original AI](http://gameinternals.com/post/2072558330/understanding-pac-man-ghost-behavior) is implemented with the help of [Game Internals](http://gameinternals.com/post/2072558330/understanding-pac-man-ghost-behavior) as well as menus, global high scores and a basic level progression system.

----

See another similar clone game: [2.5D Minesweeper](https://github.com/vilbeyli/Minesweeper)

-------------------------
Notes:

Game Over Message Duration: The 2-second delay (WaitForSeconds(2)) can be adjusted (e.g., 3 seconds) if the Game Over message needs more visibility:

yield return new WaitForSeconds(3); // Increase as needed


----------------------

PlayerPrefs: SubmitScore clears PlayerPrefs. Remove if needed:

PlayerPrefs.DeleteKey("PlayerName");
PlayerPrefs.DeleteKey("PlayerEmail");
PlayerPrefs.DeleteKey("PlayerScore");
PlayerPrefs.Save();

------------------------

Sound Overlap: Prevent overlap in PlayDeathSound:

private float lastDeathSoundTime;
public void PlayDeathSound()
{
    if (deathSound != null && Time.time - lastDeathSoundTime > deathSound.length)
    {
        audioSource.PlayOneShot(deathSound);
        lastDeathSoundTime = Time.time;
    }
}


