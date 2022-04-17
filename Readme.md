<h1 align="center">
	Nupkg Deterministicator 
</h1>

<p align="center">
    Run this tools on your NuGet Packages so they become deterministic.
</p>

# Quickstart

Install it with
```
dotnet tool install -g NupkgDeterministicator
```
Then run it on your nupkg with
```
NupkgDeterministicator <nupkg path> (optional date)
```
Your nupkg will be modified to become deterministic (your build output must be deterministic too or it will be useless).

Basically, it replaces randoms ID with a deterministic one, and changes the build date to a fixed one.

# Why ?

Supply chain attacks are becoming more and more commons.  
Reproducible builds allow to easily check if the distributed binaries are the product of the shared sources.

More at https://reproducible-builds.org/.

# So why NuGet don't allow you to do it ?

NuGet did implement the feature, but [rolled it back](https://github.com/NuGet/Home/issues/8599) soon after, because it was a breaking change for some deploy tool.  
Basically, because the date of the dll is older or equal to the dll deployed, the tool doesn't deploy it.

Now the NuGet team doesn't want to enable the feature, [even with an user-provided datetime](https://github.com/NuGet/Home/issues/8601#issuecomment-770250302), because improper usage(hardcoding the date) will cause issues to some deploy tool.

Well, personally, I don't care about these deploy tool, so I will use a fixed date.  
If you care about it, the fix is easy: Take the date of the commit your are building from.

# Credits

The script come from this repo:   
https://github.com/Thealexbarney/LibHac/blob/master/build/Build.cs  
I removed the dependencies and packed it as a dotnet tool.


