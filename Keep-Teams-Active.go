package main

import (
	"fmt"
	"time"

	"github.com/go-vgo/robotgo"
)

func main() {
	for {
		// Simulate Scroll Lock key press twice (to ensure as little interference as possible!)
		robotgo.KeyTap("scrolllock")
		robotgo.KeyTap("scrolllock")

		// Let the user know.
		fmt.Println("Continuing to keep Teams active. See you in 3 minutes!")

		// Wait for 3 minutes.
		time.Sleep(3 * time.Minute)
	}
}
