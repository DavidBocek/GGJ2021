using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public static class ErrorUtil {

	public static void InvalidSetupErrorNullField(Component c, string fieldName)
	{
		throw new UnityException(string.Format("Invalid setup on {0}. Field \"{1}\" is null and no valid target could be found in children.", c, fieldName));
	}
}
