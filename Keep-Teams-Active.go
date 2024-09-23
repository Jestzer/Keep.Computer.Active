package main

import (
	"fmt"
	"os"
	"time"

	"github.com/go-vgo/robotgo"
)

func main() {

	// Print version number, if requested.
	args := os.Args[1:]
	for _, arg := range args {
		if arg == "-version" {
			fmt.Println("Version number: 1.0")
			os.Exit(0)
		}
	}

	for {
		robotgo.KeyTap("f13")

		// Let the user know the key press was successful.
		fmt.Println("Continuing to keep Teams active. See you in 3 minutes!")

		// Wait for 3 minutes.
		time.Sleep(3 * time.Minute)
	}
}
