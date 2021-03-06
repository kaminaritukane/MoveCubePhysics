﻿using Improbable.Gdk.Core;
using Improbable.Worker.CInterop.Alpha;
using System;
using System.Collections.Generic;
using System.Linq;

public class ChosenDeploymentLocatorFlow : LocatorFlow
{
    private readonly string targetDeployment;

    public ChosenDeploymentLocatorFlow(string targetDeployment,
        IConnectionFlowInitializer<LocatorFlow> initializer = null) : base(initializer)
    {
        this.targetDeployment = targetDeployment;
    }

    protected override string SelectLoginToken(List<LoginTokenDetails> loginTokens)
    {
        var token = loginTokens.FirstOrDefault(loginToken => loginToken.DeploymentName == targetDeployment);

        return token.LoginToken ?? throw new ArgumentException("Was not able to connect to deployment");
    }
}