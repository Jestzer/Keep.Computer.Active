package main

import (
	"fmt"
	"os"
	"runtime"
	"time"

	"github.com/go-vgo/robotgo"
)

func main() {

	// Print version number, if requested.
	args := os.Args[1:]
	for _, arg := range args {
		if arg == "-version" {
			fmt.Println("Version number: 1.1")
			os.Exit(0)
		}
	}

	for {
		// Linux gets angerryyyy when you try to use F13.
		if runtime.GOOS == "linux" {
			robotgo.KeyTap("scrolllock")
			robotgo.KeyTap("scrolllock")
		} else {
			robotgo.KeyTap("f13")
		}

		// Let the user know the key press was successful.
		fmt.Println("Continuing to keep computer active. See you in 3 minutes!")

		// Wait for 3 minutes.
		time.Sleep(3 * time.Minute)
	}
}
