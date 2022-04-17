<h1 align="center">
	Nupkg Deterministicator 
</h1>

<p align="center">
    Run this tools on your NuGet Packages so they become deterministic.
</p>

# Quickstart

Install it with
```
dotnet tool install -g Kuinox.NupkgDeterministicator
```
Then run it on your nupkg with
```
NupkgDeterministicator <nupkg path> (optional date)
```
Your nupkg will be modified to become deterministic (your build output must be deterministic too or it will be useless).

Basically, it replaces randoms ID with a deterministic one, and changes the build date to a fixed one.

# Check that it works

Build your nupkg multiples times, then run NupkgDeterministicator on it, the Hash should now be the same.

```
PS C:\dev\CK\CK-Core-Projects\CK-Core\CK.Core\bin\Debug> gci | get-FileHash

Algorithm       Hash                                                                   Path
---------       ----                                                                   ----
SHA256          632DB10FB989FB7F2DB1061711D2EF5518009EA90F95E0D8BC80DDE98C4B7437       C:\dev\CK\CK-Core-Projects\CK-Core\CK.Core\bin\Debug\CK.Core.0.0.0-0-build0.nupkg
SHA256          CC46AA6BB33941739ABDCB51C1E1F43ED1E5537A7E1C1BA345EDC074C3254B72       C:\dev\CK\CK-Core-Projects\CK-Core\CK.Core\bin\Debug\CK.Core.0.0.0-0-build1.nupkg


PS C:\dev\CK\CK-Core-Projects\CK-Core\CK.Core\bin\Debug> NupkgDeterministicator .\CK.Core.0.0.0-0-build0.nupkg
PS C:\dev\CK\CK-Core-Projects\CK-Core\CK.Core\bin\Debug> NupkgDeterministicator .\CK.Core.0.0.0-0-build1.nupkg
PS C:\dev\CK\CK-Core-Projects\CK-Core\CK.Core\bin\Debug> gci | get-FileHash

Algorithm       Hash                                                                   Path
---------       ----                                                                   ----
SHA256          6C78298D641A118C95F6C1A6BFB5C3832AACB55E01F902EF988E92F9A98DA151       C:\dev\CK\CK-Core-Projects\CK-Core\CK.Core\bin\Debug\CK.Core.0.0.0-0-build0.nupkg
SHA256          6C78298D641A118C95F6C1A6BFB5C3832AACB55E01F902EF988E92F9A98DA151       C:\dev\CK\CK-Core-Projects\CK-Core\CK.Core\bin\Debug\CK.Core.0.0.0-0-build1.nupkg
```

# It doesn't work!

Make sure that your DLLs & content is deterministic!
If you have still have problems, open an issue.

# Why?

Supply chain attacks are becoming more and more commons.  
Reproducible builds allow to easily check if the distributed binaries are the product of the shared sources.

More at https://reproducible-builds.org/.

# So why NuGet don't allow you to do it?

NuGet did implement the feature, but [rolled it back](https://github.com/NuGet/Home/issues/8599) soon after, because it was a breaking change for some deploy tool.  
Basically, because the date of the dll is older or equal to the dll deployed, the tool doesn't deploy it.

Now the NuGet team doesn't want to enable the feature, [even with an user-provided datetime](https://github.com/NuGet/Home/issues/8601#issuecomment-770250302), because improper usage(hardcoding the date) will cause issues to these (faulty) deploy tool.

Well, personally, I don't care about these deploy tool, so I will use a fixed date.  
If you care about it, the fix is easy: Take the date of the commit your are building from.

# Credits

The script come from this repo:   
https://github.com/Thealexbarney/LibHac/blob/master/build/Build.cs  
I removed the dependencies and packed it as a dotnet tool.


