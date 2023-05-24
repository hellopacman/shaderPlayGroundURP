using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HumanoidIK : MonoBehaviour
{
    public Transform lookAtTarget;
    public float lookAtWeight = 1;
    public float lookAtBodyWeight = 1;
    public float lookAtHeadWeight = 1;
    public float lookAtEyeWeight = 1;

    public Transform leftFootPos;

    private Animator _animator;

    // Start is called before the first frame update
    void Start()
    {
        _animator = GetComponent<Animator>();
    }


    private void OnAnimatorIK(int layerIndex)
    {
        if (lookAtTarget != null)
        {
            _animator.SetLookAtPosition(lookAtTarget.position);
            _animator.SetLookAtWeight(lookAtWeight, lookAtBodyWeight, lookAtHeadWeight, lookAtEyeWeight);
        }

        if (leftFootPos != null)
        {
            _animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, 1);
            _animator.SetIKPosition(AvatarIKGoal.LeftFoot, leftFootPos.position);
        }

    }

    // Update is called once per frame
}
