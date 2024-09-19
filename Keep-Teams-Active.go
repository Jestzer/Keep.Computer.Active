package main

import (
	"fmt"
	"time"

	"github.com/go-vgo/robotgo"
)

func main() {
	for {
		robotgo.KeyTap("f13")

		// Let the user know the key press was successful.
		fmt.Println("Continuing to keep Teams active. See you in 3 minutes!")

		// Wait for 3 minutes.
		time.Sleep(3 * time.Minute)
	}
}
