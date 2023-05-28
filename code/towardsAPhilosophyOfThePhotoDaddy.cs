/*using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;*/

using System;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Drawing;
using GTA.Native;
using GTA;
using GTA.Math;
using System.Media;

namespace towardsAPhilosophyOfThePhotoDaddy
{
    public class towardsAPhilosophyOfThePhotoDaddy : Script
    {
        //NPCs stuff
        static float maxDistance = 300f; //define radius within which we swap the game NPCs with photo daddy NPCs
        Ped[] NearbyPeds = World.GetNearbyPeds(Game.Player.Character, maxDistance); //an array to get all NPCs within the radius
        List<Ped> myPeds = new List<Ped>(); //a list to store the photo daddies NPCs
        Vector3 spawnLoc = new Vector3(); //a variable to store the location of the NPC to be replaced
        Model pedModel = PedHash.Tourist01AMM;//the model of the photo daddy NPC 
        bool photodaddies = false; 
        int maxPeds = 150; //the max amount of photo daddies NPCs allowed in the list
        int RELATIONSHIP_PHOTODADDY = World.AddRelationshipGroup("PHOTODADDY"); //we "mark" the photodaddy NPC with this relationship int
        
        //random quotes stuff
        SoundPlayer quote = new SoundPlayer(); //audio player
        bool FuncInit = false; //bool to start the timer
        int TimerCountdown; //timer to trigger the audio

        public towardsAPhilosophyOfThePhotoDaddy()////////////////////////////////////////////////////////////////////
        {
            Tick += onTick;
            KeyUp += onKeyUp;
        }

        private void onKeyUp(object sender, KeyEventArgs e)///////////////////////////////////////////////////////////
        {
            //toggle the mod by pressing L
            if (e.KeyCode == Keys.L)
            {
                //the mod cannot be activated if we are on a mission
                if (Function.Call<bool>(Hash.GET_MISSION_FLAG) == true)
                {
                    UI.Notify("photo daddies are unavailable during missions");
                }
                else
                {
                    photodaddies = !photodaddies; //toggle the boolean

                    if (photodaddies) //turn on the mod
                    {
                        UI.Notify("photo daddies ON");
                        //Set golden hour time and clear weather
                        Function.Call(Hash.SET_CLOCK_TIME, 19, 49, 00);
                        Function.Call(Hash.SET_WEATHER_TYPE_NOW_PERSIST, "EXTRASUNNY");
                        Function.Call(Hash.PAUSE_CLOCK, true);
                        FuncInit = false;
                    }
                    else
                    {
                        Game.Player.Character.Task.ClearAllImmediately(); //clear any task from the player character
                        UI.Notify("photo daddies OFF");
                        Function.Call(Hash.PAUSE_CLOCK, false); //restart clock
                        //delete all the photodaddies NPCs and clear the list
                        if (myPeds.Count > 0)
                        {
                            for (int i = 0; i < myPeds.Count; i++)
                            {
                                if (myPeds[i] != null)
                                {
                                    myPeds[i].Delete();
                                }
                            }
                            myPeds.Clear();
                        }
                    }
                }
            }
        }

        private void onTick(object sender, EventArgs e)///////////////////////////////////////////////////////////////
        {
            //turn off the mod if we are on a mission or if we start one
            if (Function.Call<bool>(Hash.GET_MISSION_FLAG) == true)
            {
                photodaddies = false;
            }
            //if the mod is on
            if (photodaddies)
            {
                //play a random quote audio file every random interval 
                int elapsedTime = (int)(Game.LastFrameTime * 1000);
                if (!FuncInit)
                {
                    Random rando = new Random();
                    int rndInterval = rando.Next(35000, 41000);
                    TimerCountdown = rndInterval;
                    FuncInit = true;
                }
                else
                {
                    TimerCountdown -= elapsedTime;

                    if (TimerCountdown < 0)
                    {

                        //play a random quote audio file
                        Random rand = new Random();
                        int rndQuote = rand.Next(1, 38);
                        var quote = new SoundPlayer("./scripts/flusser/" + rndQuote + ".wav");
                        quote.Play();
                        FuncInit = false;
                    }
                }

                //First we get all nearby peds and add them to our array
                NearbyPeds = World.GetNearbyPeds(Game.Player.Character, maxDistance);
                foreach (Ped p in NearbyPeds)
                {
                    //if the ped is not already a photodaddy (relationship not set) and is not in a car
                    if (p.RelationshipGroup != RELATIONSHIP_PHOTODADDY /*&& p.IsInVehicle() == false*/ && p.IsHuman == true)
                    {

                        //if the list of photodaddies is not full
                        if (myPeds.Count < maxPeds)
                        {
                            //save each ped's position
                            spawnLoc = p.Position;
                            //delete the ped
                            p.Delete();
                            //create a new ped in the same position with a photo daddy model
                            var newPed = World.CreatePed(pedModel, spawnLoc);
                            //add photodaddy relationship
                            newPed.RelationshipGroup = RELATIONSHIP_PHOTODADDY;
                            //set ped invincible
                            Function.Call(Hash.SET_ENTITY_INVINCIBLE, newPed, true);
                            //add the ped to the list of photodaddy peds
                            myPeds.Add(newPed);
                        }
                        //if it's full
                        else
                        {
                            //go through the distances of each currently existing photo daddy ped
                            List<float> pedDists = new List<float>();
                            for (int i = 0; i < myPeds.Count; i++)
                            {
                                pedDists.Add(myPeds[i].Position.DistanceTo2D(Game.Player.Character.Position));
                            }
                            //sort it from shortest to furtherst
                            pedDists.Sort();

                            //go through all photo daddy peds
                            for (int i = 0; i < myPeds.Count; i++)
                            {
                                //find the photo daddy that matches the furthest position from the player
                                if (myPeds[i].Position.DistanceTo2D(Game.Player.Character.Position) >= (int)pedDists[myPeds.Count - 1])
                                {
                                    //save each ped's position
                                    spawnLoc = p.Position;
                                    //delete the ped
                                    p.Delete();
                                    //create a new ped in the same position with a photo daddy model
                                    var newPed = World.CreatePed(pedModel, spawnLoc);
                                    //add photodaddy relationship
                                    newPed.RelationshipGroup = RELATIONSHIP_PHOTODADDY;
                                    //set the photo daddy invincible
                                    Function.Call(Hash.SET_ENTITY_INVINCIBLE, newPed, true);

                                    //delete it from the list
                                    myPeds[i].Delete();
                                    //add the new ped to the list in its place
                                    myPeds[i] = newPed;
                                    //UI.Notify("SWAPPED PED");
                                }
                            }
                            //UI.Notify("Updated total no. of ped = " + myPeds.Count);
                        }
                    }
                }

                //here we go through all the photo daddy NPCs and check that they are taking pics
                if (myPeds.Count > 0)
                {
                    //go through all photo daddies 
                    for (int i = 0; i < myPeds.Count; i++)
                    {
                        //if they are not taking pics
                        if (Function.Call<bool>(Hash.GET_IS_TASK_ACTIVE, myPeds[i], 118) == false) //in TaskIndex 118 is CTaskUseScenario which seems to work
                        {
                            //set a random heading so they are not all facing the same direction
                            Random rnd = new Random();
                            int rndHeading = rnd.Next(-180, 180);
                            //make the ped take pictures
                            myPeds[i].Task.StartScenario("WORLD_HUMAN_PAPARAZZI", myPeds[i].Position, rndHeading);
                            Function.Call(Hash.SET_PED_KEEP_TASK, myPeds[i], true);
                        }
                    }
                }
            }

        }
    }
}
