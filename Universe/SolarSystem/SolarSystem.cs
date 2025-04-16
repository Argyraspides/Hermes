/*




88        88  88888888888  88888888ba   88b           d88  88888888888  ad88888ba
88        88  88           88      "8b  888b         d888  88          d8"     "8b
88        88  88           88      ,8P  88`8b       d8'88  88          Y8,
88aaaaaaaa88  88aaaaa      88aaaaaa8P'  88 `8b     d8' 88  88aaaaa     `Y8aaaaa,
88""""""""88  88"""""      88""""88'    88  `8b   d8'  88  88"""""       `"""""8b,
88        88  88           88    `8b    88   `8b d8'   88  88                  `8b
88        88  88           88     `8b   88    `888'    88  88          Y8a     a8P
88        88  88888888888  88      `8b  88     `8'     88  88888888888  "Y88888P"


                            MESSENGER OF THE MACHINES

*/

using Godot;
using System;
using Hermes.Core.StateManagers;

namespace Hermes.Universe.SolarSystem
{
    public partial class SolarSystem : Node3D
    {

        StaticBody3D earth;
        InputManager inputManager;

        public override void _Ready()
        {
            earth = GetNode<StaticBody3D>("Earth");
            inputManager = GetNode<InputManager>("InputManager");

            // TODO::ARGYRASPIDES() { We shouldn't be doing this in the solar system lol }
            DisplayServer.WindowSetMinSize(
                new Vector2I(
                    HermesSettings.MINIMUM_SCREEN_WIDTH,
                    HermesSettings.MINIMUM_SCREEN_HEIGHT));
        }

    }
}

